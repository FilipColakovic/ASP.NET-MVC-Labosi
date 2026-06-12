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
    [Route("api/couriers")]
    public class CouriersApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CouriersApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult<IEnumerable<CourierResponseDto>> GetAll([FromQuery] string? q, [FromQuery] int take = 200)
        {
            var cappedTake = Math.Clamp(take, 1, 500);
            var query = _context.Couriers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.FirstName, $"%{search}%") ||
                    EF.Functions.Like(x.LastName, $"%{search}%") ||
                    EF.Functions.Like(x.Email, $"%{search}%") ||
                    EF.Functions.Like(x.PhoneNumber, $"%{search}%") ||
                    EF.Functions.Like(x.VehicleType, $"%{search}%") ||
                    EF.Functions.Like(x.LicensePlate, $"%{search}%"));
            }

            var data = query
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Take(cappedTake)
                .Select(x => new CourierResponseDto
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,
                    VehicleType = x.VehicleType,
                    LicensePlate = x.LicensePlate,
                    IsAvailable = x.IsAvailable
                })
                .ToList();

            return Ok(data);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<CourierResponseDto> GetById(int id)
        {
            var model = _context.Couriers
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == id);

            if (model is null)
            {
                return NotFound();
            }

            return Ok(new CourierResponseDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                VehicleType = model.VehicleType,
                LicensePlate = model.LicensePlate,
                IsAvailable = model.IsAvailable
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<CourierResponseDto> Create([FromBody] CourierUpsertRequestDto request)
        {
            var normalizedEmail = request.Email.Trim();
            var normalizedPlate = request.LicensePlate.Trim();

            var emailExists = _context.Couriers.IgnoreQueryFilters()
                .Any(x => x.Email.ToLower() == normalizedEmail.ToLower());
            if (emailExists)
            {
                ModelState.AddModelError(nameof(request.Email), "Email already exists.");
            }

            var plateExists = _context.Couriers.IgnoreQueryFilters()
                .Any(x => x.LicensePlate.ToLower() == normalizedPlate.ToLower());
            if (plateExists)
            {
                ModelState.AddModelError(nameof(request.LicensePlate), "License plate already exists.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var model = new Courier
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = normalizedEmail,
                PhoneNumber = request.PhoneNumber.Trim(),
                VehicleType = request.VehicleType.Trim(),
                LicensePlate = normalizedPlate,
                IsAvailable = request.IsAvailable
            };

            _context.Couriers.Add(model);
            _context.SaveChanges();

            var response = new CourierResponseDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                VehicleType = model.VehicleType,
                LicensePlate = model.LicensePlate,
                IsAvailable = model.IsAvailable
            };

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, response);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<CourierResponseDto> Update(int id, [FromBody] CourierUpsertRequestDto request)
        {
            if (request.Id != 0 && request.Id != id)
            {
                return BadRequest(new { message = "Route id and body id do not match." });
            }

            var model = _context.Couriers.FirstOrDefault(x => x.Id == id);
            if (model is null)
            {
                return NotFound();
            }

            var normalizedEmail = request.Email.Trim();
            var normalizedPlate = request.LicensePlate.Trim();

            var emailExists = _context.Couriers.IgnoreQueryFilters()
                .Any(x => x.Email.ToLower() == normalizedEmail.ToLower() && x.Id != id);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(request.Email), "Email already exists.");
            }

            var plateExists = _context.Couriers.IgnoreQueryFilters()
                .Any(x => x.LicensePlate.ToLower() == normalizedPlate.ToLower() && x.Id != id);
            if (plateExists)
            {
                ModelState.AddModelError(nameof(request.LicensePlate), "License plate already exists.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            model.FirstName = request.FirstName.Trim();
            model.LastName = request.LastName.Trim();
            model.Email = normalizedEmail;
            model.PhoneNumber = request.PhoneNumber.Trim();
            model.VehicleType = request.VehicleType.Trim();
            model.LicensePlate = normalizedPlate;
            model.IsAvailable = request.IsAvailable;

            _context.SaveChanges();

            return Ok(new CourierResponseDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                VehicleType = model.VehicleType,
                LicensePlate = model.LicensePlate,
                IsAvailable = model.IsAvailable
            });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var model = _context.Couriers.FirstOrDefault(x => x.Id == id);
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


