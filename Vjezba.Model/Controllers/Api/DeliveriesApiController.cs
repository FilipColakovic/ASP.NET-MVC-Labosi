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
    [Route("api/deliveries")]
    public class DeliveriesApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DeliveriesApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult<IEnumerable<DeliveryResponseDto>> GetAll([FromQuery] string? q, [FromQuery] int take = 200)
        {
            var cappedTake = Math.Clamp(take, 1, 500);
            var query = _context.Deliveries
                .AsNoTracking()
                .Include(x => x.Courier)
                .Include(x => x.Packages)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.CurrentLocation, $"%{search}%") ||
                    EF.Functions.Like(x.Courier.FirstName, $"%{search}%") ||
                    EF.Functions.Like(x.Courier.LastName, $"%{search}%"));
            }

            var data = query
                .OrderByDescending(x => x.DepartureDate)
                .Take(cappedTake)
                .ToList()
                .Select(ToDto)
                .ToList();

            return Ok(data);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<DeliveryResponseDto> GetById(int id)
        {
            var model = _context.Deliveries
                .AsNoTracking()
                .Include(x => x.Courier)
                .Include(x => x.Packages)
                .FirstOrDefault(x => x.Id == id);

            if (model is null)
            {
                return NotFound();
            }

            return Ok(ToDto(model));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<DeliveryResponseDto> Create([FromBody] DeliveryUpsertRequestDto request)
        {
            var courier = _context.Couriers.AsNoTracking().FirstOrDefault(x => x.Id == request.CourierId);
            if (courier is null)
            {
                ModelState.AddModelError(nameof(request.CourierId), "Courier not found.");
                return ValidationProblem(ModelState);
            }

            var model = new Delivery
            {
                DepartureDate = request.DepartureDate,
                ArrivalDate = request.ArrivalDate,
                CurrentLocation = request.CurrentLocation.Trim(),
                IsDelayed = request.IsDelayed,
                CourierId = request.CourierId
            };

            _context.Deliveries.Add(model);
            _context.SaveChanges();

            model.Courier = courier;

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, ToDto(model));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<DeliveryResponseDto> Update(int id, [FromBody] DeliveryUpsertRequestDto request)
        {
            if (request.Id != 0 && request.Id != id)
            {
                return BadRequest(new { message = "Route id and body id do not match." });
            }

            var model = _context.Deliveries
                .Include(x => x.Courier)
                .Include(x => x.Packages)
                .FirstOrDefault(x => x.Id == id);
            if (model is null)
            {
                return NotFound();
            }

            var courier = _context.Couriers.AsNoTracking().FirstOrDefault(x => x.Id == request.CourierId);
            if (courier is null)
            {
                ModelState.AddModelError(nameof(request.CourierId), "Courier not found.");
                return ValidationProblem(ModelState);
            }

            model.DepartureDate = request.DepartureDate;
            model.ArrivalDate = request.ArrivalDate;
            model.CurrentLocation = request.CurrentLocation.Trim();
            model.IsDelayed = request.IsDelayed;
            model.CourierId = request.CourierId;

            _context.SaveChanges();

            model.Courier = courier;

            return Ok(ToDto(model));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var model = _context.Deliveries.FirstOrDefault(x => x.Id == id);
            if (model is null)
            {
                return NotFound();
            }

            model.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return NoContent();
        }

        private static DeliveryResponseDto ToDto(Delivery model)
        {
            return new DeliveryResponseDto
            {
                Id = model.Id,
                DepartureDate = model.DepartureDate,
                ArrivalDate = model.ArrivalDate,
                CurrentLocation = model.CurrentLocation,
                IsDelayed = model.IsDelayed,
                Courier = model.Courier?.ToSummaryDto(),
                Packages = model.Packages
                    .OrderBy(x => x.TrackingNumber)
                    .Select(x => x.ToSummaryDto())
                    .ToList()
            };
        }
    }
}


