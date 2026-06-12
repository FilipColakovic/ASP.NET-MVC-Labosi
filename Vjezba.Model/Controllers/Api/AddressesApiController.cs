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
    [Route("api/addresses")]
    public class AddressesApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AddressesApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult<IEnumerable<AddressResponseDto>> GetAll([FromQuery] string? q, [FromQuery] int take = 200)
        {
            var cappedTake = Math.Clamp(take, 1, 500);
            var query = _context.Addresses.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.Street, $"%{search}%") ||
                    EF.Functions.Like(x.City, $"%{search}%") ||
                    EF.Functions.Like(x.PostalCode, $"%{search}%") ||
                    EF.Functions.Like(x.Country, $"%{search}%"));
            }

            var data = query
                .OrderBy(x => x.City)
                .ThenBy(x => x.Street)
                .Take(cappedTake)
                .Select(x => new AddressResponseDto
                {
                    Id = x.Id,
                    Street = x.Street,
                    City = x.City,
                    PostalCode = x.PostalCode,
                    Country = x.Country
                })
                .ToList();

            return Ok(data);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<AddressResponseDto> GetById(int id)
        {
            var model = _context.Addresses
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == id);

            if (model is null)
            {
                return NotFound();
            }

            return Ok(new AddressResponseDto
            {
                Id = model.Id,
                Street = model.Street,
                City = model.City,
                PostalCode = model.PostalCode,
                Country = model.Country
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<AddressResponseDto> Create([FromBody] AddressUpsertRequestDto request)
        {
            var model = new Address
            {
                Street = request.Street.Trim(),
                City = request.City.Trim(),
                PostalCode = request.PostalCode.Trim(),
                Country = request.Country.Trim()
            };

            _context.Addresses.Add(model);
            _context.SaveChanges();

            var response = new AddressResponseDto
            {
                Id = model.Id,
                Street = model.Street,
                City = model.City,
                PostalCode = model.PostalCode,
                Country = model.Country
            };

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, response);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<AddressResponseDto> Update(int id, [FromBody] AddressUpsertRequestDto request)
        {
            if (request.Id != 0 && request.Id != id)
            {
                return BadRequest(new { message = "Route id and body id do not match." });
            }

            var model = _context.Addresses.FirstOrDefault(x => x.Id == id);
            if (model is null)
            {
                return NotFound();
            }

            model.Street = request.Street.Trim();
            model.City = request.City.Trim();
            model.PostalCode = request.PostalCode.Trim();
            model.Country = request.Country.Trim();

            _context.SaveChanges();

            return Ok(new AddressResponseDto
            {
                Id = model.Id,
                Street = model.Street,
                City = model.City,
                PostalCode = model.PostalCode,
                Country = model.Country
            });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var model = _context.Addresses.FirstOrDefault(x => x.Id == id);
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


