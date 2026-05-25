using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vjezba.Model.Data;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers
{
    [Route("status-logs")]
    public class StatusLogsController : Controller
    {
        private readonly AppDbContext _context;

        public StatusLogsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{id:int}/soft-delete")]
        [Authorize(Roles = "Admin")]
        public IActionResult SoftDelete(int id)
        {
            var statusLog = _context.StatusLogs.FirstOrDefault(x => x.Id == id);
            if (statusLog is null)
            {
                return NotFound();
            }

            statusLog.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return RedirectToAction("Index", "Home", new { selectedType = "statuslog" });
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create(StatusLogCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var package = _context.Packages.FirstOrDefault(x => x.Id == model.PackageId);
            if (package is null)
            {
                ModelState.AddModelError(nameof(StatusLogCreateViewModel.PackageId), "Package not found.");
                return BadRequest(new { errors = BuildErrors() });
            }

            var statusLog = new StatusLog
            {
                TimeChanged = model.TimeChanged,
                Location = model.Location.Trim(),
                Description = model.Description.Trim(),
                PreviousStatus = model.PreviousStatus,
                NewStatus = model.NewStatus,
                PackageId = model.PackageId
            };

            _context.StatusLogs.Add(statusLog);
            _context.SaveChanges();

            return Ok(new { id = statusLog.Id });
        }

        [HttpPost("edit")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Edit(StatusLogEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var statusLog = _context.StatusLogs.FirstOrDefault(x => x.Id == model.Id);
            if (statusLog is null)
            {
                return NotFound();
            }

            var package = _context.Packages.FirstOrDefault(x => x.Id == model.PackageId);
            if (package is null)
            {
                ModelState.AddModelError(nameof(StatusLogEditViewModel.PackageId), "Package not found.");
                return BadRequest(new { errors = BuildErrors() });
            }

            statusLog.TimeChanged = model.TimeChanged;
            statusLog.Location = model.Location.Trim();
            statusLog.Description = model.Description.Trim();
            statusLog.PreviousStatus = model.PreviousStatus;
            statusLog.NewStatus = model.NewStatus;
            statusLog.PackageId = model.PackageId;

            _context.SaveChanges();

            return Ok(new { id = statusLog.Id });
        }

        private IDictionary<string, string[]> BuildErrors()
        {
            return ModelState
                .Where(entry => entry.Value is not null && entry.Value.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.Errors.Select(error => error.ErrorMessage).ToArray() ?? Array.Empty<string>());
        }
    }
}
