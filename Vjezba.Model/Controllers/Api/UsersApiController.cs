using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vjezba.Model.Data;
using Vjezba.Model.Dtos.Api;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers.Api
{
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Route("api/users")]
    public class UsersApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult<IEnumerable<UserResponseDto>> GetAll([FromQuery] string? q, [FromQuery] int take = 200)
        {
            var cappedTake = Math.Clamp(take, 1, 500);
            var query = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.FirstName, $"%{search}%") ||
                    EF.Functions.Like(x.LastName, $"%{search}%") ||
                    EF.Functions.Like(x.Email, $"%{search}%") ||
                    EF.Functions.Like(x.PhoneNumber, $"%{search}%"));
            }

            var data = query
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Take(cappedTake)
                .Select(x => new UserResponseDto
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,
                    RegistrationDate = x.RegistrationDate
                })
                .ToList();

            return Ok(data);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<UserResponseDto> GetById(int id)
        {
            var model = _context.Users
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == id);

            if (model is null)
            {
                return NotFound();
            }

            return Ok(new UserResponseDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                RegistrationDate = model.RegistrationDate
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<UserResponseDto> Create([FromBody] UserUpsertRequestDto request)
        {
            var normalizedEmail = request.Email.Trim();
            var emailExists = _context.Users.IgnoreQueryFilters()
                .Any(x => x.Email.ToLower() == normalizedEmail.ToLower());
            if (emailExists)
            {
                ModelState.AddModelError(nameof(request.Email), "Email already exists.");
                return ValidationProblem(ModelState);
            }

            var model = new User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = normalizedEmail,
                PhoneNumber = request.PhoneNumber.Trim(),
                RegistrationDate = DateTime.UtcNow
            };

            _context.Users.Add(model);
            _context.SaveChanges();

            var response = new UserResponseDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                RegistrationDate = model.RegistrationDate
            };

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, response);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<UserResponseDto> Update(int id, [FromBody] UserUpsertRequestDto request)
        {
            if (request.Id != 0 && request.Id != id)
            {
                return BadRequest(new { message = "Route id and body id do not match." });
            }

            var model = _context.Users.FirstOrDefault(x => x.Id == id);
            if (model is null)
            {
                return NotFound();
            }

            var normalizedEmail = request.Email.Trim();
            var emailExists = _context.Users.IgnoreQueryFilters()
                .Any(x => x.Email.ToLower() == normalizedEmail.ToLower() && x.Id != id);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(request.Email), "Email already exists.");
                return ValidationProblem(ModelState);
            }

            model.FirstName = request.FirstName.Trim();
            model.LastName = request.LastName.Trim();
            model.Email = normalizedEmail;
            model.PhoneNumber = request.PhoneNumber.Trim();

            _context.SaveChanges();

            return Ok(new UserResponseDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                RegistrationDate = model.RegistrationDate
            });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var model = _context.Users.FirstOrDefault(x => x.Id == id);
            if (model is null)
            {
                return NotFound();
            }

            model.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return NoContent();
        }
    }
}


