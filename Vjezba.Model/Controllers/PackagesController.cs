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
        private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PackagesController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

            return RedirectToAction("Manifest", "Home", new { selectedType = "package" });
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

        [HttpGet("{packageId:int}/attachments")]
        [Authorize]
        public async Task<IActionResult> ListAttachments(int packageId)
        {
            var packageExists = await _context.Packages.AnyAsync(x => x.Id == packageId);
            if (!packageExists)
            {
                return NotFound();
            }

            var attachments = await _context.Attachments
                .AsNoTracking()
                .Where(x => x.PackageId == packageId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.FileName,
                    Url = "/" + x.FilePath,
                    x.ContentType,
                    x.FileSize,
                    x.CreatedAt
                })
                .ToListAsync();

            return Ok(attachments);
        }

        [HttpPost("{packageId:int}/attachments")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UploadAttachment(int packageId, IFormFile? file)
        {
            var packageExists = await _context.Packages.AnyAsync(x => x.Id == packageId);
            if (!packageExists)
            {
                return NotFound();
            }

            if (file is null || file.Length == 0)
            {
                return BadRequest(new { message = "Choose a file before uploading." });
            }

            if (file.Length > MaxAttachmentSizeBytes)
            {
                return BadRequest(new { message = "Maximum upload size is 10 MB." });
            }

            var originalFileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                originalFileName = "attachment";
            }

            var extension = Path.GetExtension(originalFileName);
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var uploadDirectory = GetPackageUploadDirectory(packageId);
            Directory.CreateDirectory(uploadDirectory);

            var physicalPath = Path.Combine(uploadDirectory, storedFileName);
            await using (var stream = new FileStream(physicalPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new Attachment
            {
                PackageId = packageId,
                FileName = originalFileName,
                FilePath = $"uploads/packages/{packageId}/{storedFileName}",
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType,
                FileSize = file.Length,
                CreatedAt = DateTime.UtcNow
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(ListAttachments),
                new { packageId },
                new
                {
                    attachment.Id,
                    attachment.FileName,
                    Url = "/" + attachment.FilePath,
                    attachment.ContentType,
                    attachment.FileSize,
                    attachment.CreatedAt
                });
        }

        [HttpDelete("{packageId:int}/attachments/{attachmentId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAttachment(int packageId, int attachmentId)
        {
            var attachment = await _context.Attachments
                .FirstOrDefaultAsync(x => x.Id == attachmentId && x.PackageId == packageId);
            if (attachment is null)
            {
                return NotFound();
            }

            var physicalPath = Path.Combine(
                GetPackageUploadDirectory(packageId),
                Path.GetFileName(attachment.FilePath));

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            return NoContent();
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

        private string GetPackageUploadDirectory(int packageId)
        {
            var webRootPath = _environment.WebRootPath
                ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

            return Path.Combine(webRootPath, "uploads", "packages", packageId.ToString());
        }
    }
}
