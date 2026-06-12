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
    [Route("api/status-logs")]
    public class StatusLogsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatusLogsApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult<IEnumerable<StatusLogResponseDto>> GetAll([FromQuery] string? q, [FromQuery] int take = 200)
        {
            var cappedTake = Math.Clamp(take, 1, 500);
            var query = _context.StatusLogs
                .AsNoTracking()
                .Include(x => x.Package)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.Package.TrackingNumber, $"%{search}%") ||
                    EF.Functions.Like(x.Location, $"%{search}%") ||
                    EF.Functions.Like(x.Description, $"%{search}%"));
            }

            var data = query
                .OrderByDescending(x => x.TimeChanged)
                .Take(cappedTake)
                .ToList()
                .Select(ToDto)
                .ToList();

            return Ok(data);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<StatusLogResponseDto> GetById(int id)
        {
            var model = _context.StatusLogs
                .AsNoTracking()
                .Include(x => x.Package)
                .FirstOrDefault(x => x.Id == id);

            if (model is null)
            {
                return NotFound();
            }

            return Ok(ToDto(model));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<StatusLogResponseDto> Create([FromBody] StatusLogUpsertRequestDto request)
        {
            var package = _context.Packages.AsNoTracking().FirstOrDefault(x => x.Id == request.PackageId);
            if (package is null)
            {
                ModelState.AddModelError(nameof(request.PackageId), "Package not found.");
                return ValidationProblem(ModelState);
            }

            var model = new StatusLog
            {
                TimeChanged = request.TimeChanged,
                Location = request.Location.Trim(),
                Description = request.Description.Trim(),
                PreviousStatus = request.PreviousStatus,
                NewStatus = request.NewStatus,
                PackageId = request.PackageId
            };

            _context.StatusLogs.Add(model);
            _context.SaveChanges();

            model.Package = package;

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, ToDto(model));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<StatusLogResponseDto> Update(int id, [FromBody] StatusLogUpsertRequestDto request)
        {
            if (request.Id != 0 && request.Id != id)
            {
                return BadRequest(new { message = "Route id and body id do not match." });
            }

            var model = _context.StatusLogs
                .Include(x => x.Package)
                .FirstOrDefault(x => x.Id == id);
            if (model is null)
            {
                return NotFound();
            }

            var package = _context.Packages.AsNoTracking().FirstOrDefault(x => x.Id == request.PackageId);
            if (package is null)
            {
                ModelState.AddModelError(nameof(request.PackageId), "Package not found.");
                return ValidationProblem(ModelState);
            }

            model.TimeChanged = request.TimeChanged;
            model.Location = request.Location.Trim();
            model.Description = request.Description.Trim();
            model.PreviousStatus = request.PreviousStatus;
            model.NewStatus = request.NewStatus;
            model.PackageId = request.PackageId;

            _context.SaveChanges();

            model.Package = package;

            return Ok(ToDto(model));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var model = _context.StatusLogs.FirstOrDefault(x => x.Id == id);
            if (model is null)
            {
                return NotFound();
            }

            model.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return NoContent();
        }

        private static StatusLogResponseDto ToDto(StatusLog model)
        {
            return new StatusLogResponseDto
            {
                Id = model.Id,
                TimeChanged = model.TimeChanged,
                Location = model.Location,
                Description = model.Description,
                PreviousStatus = model.PreviousStatus,
                NewStatus = model.NewStatus,
                Package = model.Package?.ToSummaryDto()
            };
        }
    }
}


