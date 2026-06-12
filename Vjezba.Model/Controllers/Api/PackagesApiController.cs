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
    [Route("api/packages")]
    public class PackagesApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PackagesApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult<IEnumerable<PackageResponseDto>> GetAll([FromQuery] string? q, [FromQuery] int take = 200)
        {
            var cappedTake = Math.Clamp(take, 1, 500);
            var query = _context.Packages
                .AsNoTracking()
                .Include(x => x.Courier)
                .Include(x => x.SenderUser)
                .Include(x => x.RecipientUser)
                .Include(x => x.SenderAddress)
                .Include(x => x.RecipientAddress)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.TrackingNumber, $"%{search}%") ||
                    EF.Functions.Like(x.Description, $"%{search}%") ||
                    EF.Functions.Like(x.RecipientAddress.City, $"%{search}%") ||
                    EF.Functions.Like(x.RecipientAddress.Country, $"%{search}%"));
            }

            var data = query
                .OrderByDescending(x => x.CreatedAt)
                .Take(cappedTake)
                .ToList()
                .Select(ToDto)
                .ToList();

            return Ok(data);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<PackageResponseDto> GetById(int id)
        {
            var model = _context.Packages
                .AsNoTracking()
                .Include(x => x.Courier)
                .Include(x => x.SenderUser)
                .Include(x => x.RecipientUser)
                .Include(x => x.SenderAddress)
                .Include(x => x.RecipientAddress)
                .FirstOrDefault(x => x.Id == id);

            if (model is null)
            {
                return NotFound();
            }

            return Ok(ToDto(model));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<PackageResponseDto> Create([FromBody] PackageUpsertRequestDto request)
        {
            ValidatePackageRequest(request, null);
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var model = new Package
            {
                TrackingNumber = request.TrackingNumber.Trim(),
                WeightKg = request.WeightKg,
                DeliveryPriority = request.DeliveryPriority,
                CourierId = request.CourierId,
                SenderUserId = request.SenderUserId,
                RecipientUserId = request.RecipientUserId,
                SenderAddressId = request.SenderAddressId,
                RecipientAddressId = request.RecipientAddressId,
                Status = request.Status,
                Description = request.Description.Trim(),
                CreatedAt = DateTime.UtcNow,
                DeliveredAt = request.DeliveredAt
            };

            _context.Packages.Add(model);
            _context.SaveChanges();

            var responseModel = LoadPackageGraph(model.Id);
            if (responseModel is null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, ToDto(responseModel));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<PackageResponseDto> Update(int id, [FromBody] PackageUpsertRequestDto request)
        {
            if (request.Id != 0 && request.Id != id)
            {
                return BadRequest(new { message = "Route id and body id do not match." });
            }

            var model = _context.Packages.FirstOrDefault(x => x.Id == id);
            if (model is null)
            {
                return NotFound();
            }

            ValidatePackageRequest(request, id);
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            model.TrackingNumber = request.TrackingNumber.Trim();
            model.WeightKg = request.WeightKg;
            model.DeliveryPriority = request.DeliveryPriority;
            model.CourierId = request.CourierId;
            model.SenderUserId = request.SenderUserId;
            model.RecipientUserId = request.RecipientUserId;
            model.SenderAddressId = request.SenderAddressId;
            model.RecipientAddressId = request.RecipientAddressId;
            model.Status = request.Status;
            model.Description = request.Description.Trim();
            model.DeliveredAt = request.DeliveredAt;

            _context.SaveChanges();

            var responseModel = LoadPackageGraph(model.Id);
            if (responseModel is null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok(ToDto(responseModel));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var model = _context.Packages.FirstOrDefault(x => x.Id == id);
            if (model is null)
            {
                return NotFound();
            }

            model.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return NoContent();
        }

        private void ValidatePackageRequest(PackageUpsertRequestDto request, int? currentId)
        {
            var normalizedTracking = request.TrackingNumber.Trim();
            var trackingExists = _context.Packages.IgnoreQueryFilters()
                .Any(x => x.TrackingNumber.ToLower() == normalizedTracking.ToLower() &&
                          (!currentId.HasValue || x.Id != currentId.Value));
            if (trackingExists)
            {
                ModelState.AddModelError(nameof(request.TrackingNumber), "Tracking number already exists.");
            }

            if (_context.Couriers.FirstOrDefault(x => x.Id == request.CourierId) is null)
            {
                ModelState.AddModelError(nameof(request.CourierId), "Courier not found.");
            }

            if (_context.Users.FirstOrDefault(x => x.Id == request.SenderUserId) is null)
            {
                ModelState.AddModelError(nameof(request.SenderUserId), "Sender user not found.");
            }

            if (_context.Users.FirstOrDefault(x => x.Id == request.RecipientUserId) is null)
            {
                ModelState.AddModelError(nameof(request.RecipientUserId), "Recipient user not found.");
            }

            if (_context.Addresses.FirstOrDefault(x => x.Id == request.SenderAddressId) is null)
            {
                ModelState.AddModelError(nameof(request.SenderAddressId), "Sender address not found.");
            }

            if (_context.Addresses.FirstOrDefault(x => x.Id == request.RecipientAddressId) is null)
            {
                ModelState.AddModelError(nameof(request.RecipientAddressId), "Recipient address not found.");
            }
        }

        private Package? LoadPackageGraph(int id)
        {
            return _context.Packages
                .AsNoTracking()
                .Include(x => x.Courier)
                .Include(x => x.SenderUser)
                .Include(x => x.RecipientUser)
                .Include(x => x.SenderAddress)
                .Include(x => x.RecipientAddress)
                .FirstOrDefault(x => x.Id == id);
        }

        private static PackageResponseDto ToDto(Package model)
        {
            return new PackageResponseDto
            {
                Id = model.Id,
                TrackingNumber = model.TrackingNumber,
                WeightKg = model.WeightKg,
                DeliveryPriority = model.DeliveryPriority,
                Status = model.Status,
                Description = model.Description,
                CreatedAt = model.CreatedAt,
                DeliveredAt = model.DeliveredAt,
                Courier = model.Courier?.ToSummaryDto(),
                SenderUser = model.SenderUser?.ToSummaryDto(),
                RecipientUser = model.RecipientUser?.ToSummaryDto(),
                SenderAddress = model.SenderAddress?.ToSummaryDto(),
                RecipientAddress = model.RecipientAddress?.ToSummaryDto()
            };
        }
    }
}


