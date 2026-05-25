using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vjezba.Model.Data;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers
{
    [Route("packages")]
    public class PackagesController : Controller
    {
        private readonly AppDbContext _context;

        public PackagesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{id:int}/soft-delete")]
        [Authorize(Roles = "Admin")]
        public IActionResult SoftDelete(int id)
        {
            var package = _context.Packages.FirstOrDefault(x => x.Id == id);
            if (package is null)
            {
                return NotFound();
            }

            package.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return RedirectToAction("Index", "Home", new { selectedType = "package" });
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create(PackageCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var normalizedTracking = model.TrackingNumber.Trim();
            var trackingExists = _context.Packages.IgnoreQueryFilters()
                .Any(x => x.TrackingNumber.ToLower() == normalizedTracking.ToLower());
            if (trackingExists)
            {
                ModelState.AddModelError(nameof(PackageCreateViewModel.TrackingNumber), "Tracking number already exists.");
            }

            if (_context.Couriers.FirstOrDefault(x => x.Id == model.CourierId) is null)
            {
                ModelState.AddModelError(nameof(PackageCreateViewModel.CourierId), "Courier not found.");
            }

            if (_context.Users.FirstOrDefault(x => x.Id == model.SenderUserId) is null)
            {
                ModelState.AddModelError(nameof(PackageCreateViewModel.SenderUserId), "Sender user not found.");
            }

            if (_context.Users.FirstOrDefault(x => x.Id == model.RecipientUserId) is null)
            {
                ModelState.AddModelError(nameof(PackageCreateViewModel.RecipientUserId), "Recipient user not found.");
            }

            if (_context.Addresses.FirstOrDefault(x => x.Id == model.SenderAddressId) is null)
            {
                ModelState.AddModelError(nameof(PackageCreateViewModel.SenderAddressId), "Sender address not found.");
            }

            if (_context.Addresses.FirstOrDefault(x => x.Id == model.RecipientAddressId) is null)
            {
                ModelState.AddModelError(nameof(PackageCreateViewModel.RecipientAddressId), "Recipient address not found.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var package = new Package
            {
                TrackingNumber = normalizedTracking,
                WeightKg = model.WeightKg,
                DeliveryPriority = model.DeliveryPriority,
                CourierId = model.CourierId,
                SenderUserId = model.SenderUserId,
                RecipientUserId = model.RecipientUserId,
                SenderAddressId = model.SenderAddressId,
                RecipientAddressId = model.RecipientAddressId,
                Status = model.Status,
                CreatedAt = DateTime.UtcNow,
                DeliveredAt = model.DeliveredAt,
                Description = model.Description.Trim()
            };

            _context.Packages.Add(package);
            _context.SaveChanges();

            return Ok(new { id = package.Id });
        }

        [HttpPost("edit")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Edit(PackageEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var package = _context.Packages.FirstOrDefault(x => x.Id == model.Id);
            if (package is null)
            {
                return NotFound();
            }

            var normalizedTracking = model.TrackingNumber.Trim();
            var trackingExists = _context.Packages.IgnoreQueryFilters()
                .Any(x => x.TrackingNumber.ToLower() == normalizedTracking.ToLower() && x.Id != model.Id);
            if (trackingExists)
            {
                ModelState.AddModelError(nameof(PackageEditViewModel.TrackingNumber), "Tracking number already exists.");
            }

            if (_context.Couriers.FirstOrDefault(x => x.Id == model.CourierId) is null)
            {
                ModelState.AddModelError(nameof(PackageEditViewModel.CourierId), "Courier not found.");
            }

            if (_context.Users.FirstOrDefault(x => x.Id == model.SenderUserId) is null)
            {
                ModelState.AddModelError(nameof(PackageEditViewModel.SenderUserId), "Sender user not found.");
            }

            if (_context.Users.FirstOrDefault(x => x.Id == model.RecipientUserId) is null)
            {
                ModelState.AddModelError(nameof(PackageEditViewModel.RecipientUserId), "Recipient user not found.");
            }

            if (_context.Addresses.FirstOrDefault(x => x.Id == model.SenderAddressId) is null)
            {
                ModelState.AddModelError(nameof(PackageEditViewModel.SenderAddressId), "Sender address not found.");
            }

            if (_context.Addresses.FirstOrDefault(x => x.Id == model.RecipientAddressId) is null)
            {
                ModelState.AddModelError(nameof(PackageEditViewModel.RecipientAddressId), "Recipient address not found.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            package.TrackingNumber = normalizedTracking;
            package.WeightKg = model.WeightKg;
            package.DeliveryPriority = model.DeliveryPriority;
            package.CourierId = model.CourierId;
            package.SenderUserId = model.SenderUserId;
            package.RecipientUserId = model.RecipientUserId;
            package.SenderAddressId = model.SenderAddressId;
            package.RecipientAddressId = model.RecipientAddressId;
            package.Status = model.Status;
            package.DeliveredAt = model.DeliveredAt;
            package.Description = model.Description.Trim();

            _context.SaveChanges();

            return Ok(new { id = package.Id });
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
