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
    [Route("api/warehouses")]
    public class WarehousesApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WarehousesApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult<IEnumerable<WarehouseResponseDto>> GetAll([FromQuery] string? q, [FromQuery] int take = 200)
        {
            var cappedTake = Math.Clamp(take, 1, 500);
            var query = _context.Warehouses
                .AsNoTracking()
                .Include(x => x.Address)
                .Include(x => x.StoredPackages)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.Name, $"%{search}%") ||
                    EF.Functions.Like(x.Address.City, $"%{search}%") ||
                    EF.Functions.Like(x.Address.Street, $"%{search}%"));
            }

            var data = query
                .OrderBy(x => x.Name)
                .Take(cappedTake)
                .ToList()
                .Select(x => new WarehouseResponseDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Capacity = x.Capacity,
                    Address = x.Address.ToSummaryDto(),
                    StoredPackageCount = x.StoredPackages.Count
                })
                .ToList();

            return Ok(data);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public ActionResult<WarehouseResponseDto> GetById(int id)
        {
            var model = _context.Warehouses
                .AsNoTracking()
                .Include(x => x.Address)
                .Include(x => x.StoredPackages)
                .FirstOrDefault(x => x.Id == id);

            if (model is null)
            {
                return NotFound();
            }

            return Ok(new WarehouseResponseDto
            {
                Id = model.Id,
                Name = model.Name,
                Capacity = model.Capacity,
                Address = model.Address.ToSummaryDto(),
                StoredPackageCount = model.StoredPackages.Count
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<WarehouseResponseDto> Create([FromBody] WarehouseUpsertRequestDto request)
        {
            var address = _context.Addresses.AsNoTracking().FirstOrDefault(x => x.Id == request.AddressId);
            if (address is null)
            {
                ModelState.AddModelError(nameof(request.AddressId), "Address not found.");
                return ValidationProblem(ModelState);
            }

            var model = new Warehouse
            {
                Name = request.Name.Trim(),
                AddressId = request.AddressId,
                Capacity = request.Capacity
            };

            _context.Warehouses.Add(model);
            _context.SaveChanges();

            return CreatedAtAction(
                nameof(GetById),
                new { id = model.Id },
                new WarehouseResponseDto
                {
                    Id = model.Id,
                    Name = model.Name,
                    Capacity = model.Capacity,
                    Address = address.ToSummaryDto(),
                    StoredPackageCount = 0
                });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<WarehouseResponseDto> Update(int id, [FromBody] WarehouseUpsertRequestDto request)
        {
            if (request.Id != 0 && request.Id != id)
            {
                return BadRequest(new { message = "Route id and body id do not match." });
            }

            var model = _context.Warehouses
                .Include(x => x.Address)
                .Include(x => x.StoredPackages)
                .FirstOrDefault(x => x.Id == id);
            if (model is null)
            {
                return NotFound();
            }

            var address = _context.Addresses.AsNoTracking().FirstOrDefault(x => x.Id == request.AddressId);
            if (address is null)
            {
                ModelState.AddModelError(nameof(request.AddressId), "Address not found.");
                return ValidationProblem(ModelState);
            }

            model.Name = request.Name.Trim();
            model.AddressId = request.AddressId;
            model.Capacity = request.Capacity;

            _context.SaveChanges();

            return Ok(new WarehouseResponseDto
            {
                Id = model.Id,
                Name = model.Name,
                Capacity = model.Capacity,
                Address = address.ToSummaryDto(),
                StoredPackageCount = model.StoredPackages.Count
            });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var model = _context.Warehouses.FirstOrDefault(x => x.Id == id);
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

