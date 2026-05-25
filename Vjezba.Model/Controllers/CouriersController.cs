using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vjezba.Model.Data;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers
{
    [Route("couriers")]
    public class CouriersController : Controller
    {
        private readonly AppDbContext _context;

        public CouriersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{id:int}/soft-delete")]
        [Authorize(Roles = "Admin")]
        public IActionResult SoftDelete(int id)
        {
            var courier = _context.Couriers.FirstOrDefault(x => x.Id == id);
            if (courier is null)
            {
                return NotFound();
            }

            courier.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return RedirectToAction("Index", "Home", new { selectedType = "courier" });
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create(CourierCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var normalizedEmail = model.Email.Trim();
            var normalizedPlate = model.LicensePlate.Trim();

            var emailExists = _context.Couriers.IgnoreQueryFilters()
                .Any(x => x.Email.ToLower() == normalizedEmail.ToLower());
            if (emailExists)
            {
                ModelState.AddModelError(nameof(CourierCreateViewModel.Email), "Email already exists.");
            }

            var plateExists = _context.Couriers.IgnoreQueryFilters()
                .Any(x => x.LicensePlate.ToLower() == normalizedPlate.ToLower());
            if (plateExists)
            {
                ModelState.AddModelError(nameof(CourierCreateViewModel.LicensePlate), "License plate already exists.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var courier = new Courier
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = normalizedEmail,
                PhoneNumber = model.PhoneNumber.Trim(),
                VehicleType = model.VehicleType.Trim(),
                LicensePlate = normalizedPlate,
                IsAvailable = model.IsAvailable
            };

            _context.Couriers.Add(courier);
            _context.SaveChanges();

            return Ok(new { id = courier.Id });
        }

        [HttpPost("edit")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Edit(CourierEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var courier = _context.Couriers.FirstOrDefault(x => x.Id == model.Id);
            if (courier is null)
            {
                return NotFound();
            }

            var normalizedEmail = model.Email.Trim();
            var normalizedPlate = model.LicensePlate.Trim();

            var emailExists = _context.Couriers.IgnoreQueryFilters()
                .Any(x => x.Email.ToLower() == normalizedEmail.ToLower() && x.Id != model.Id);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(CourierEditViewModel.Email), "Email already exists.");
            }

            var plateExists = _context.Couriers.IgnoreQueryFilters()
                .Any(x => x.LicensePlate.ToLower() == normalizedPlate.ToLower() && x.Id != model.Id);
            if (plateExists)
            {
                ModelState.AddModelError(nameof(CourierEditViewModel.LicensePlate), "License plate already exists.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            courier.FirstName = model.FirstName.Trim();
            courier.LastName = model.LastName.Trim();
            courier.Email = normalizedEmail;
            courier.PhoneNumber = model.PhoneNumber.Trim();
            courier.VehicleType = model.VehicleType.Trim();
            courier.LicensePlate = normalizedPlate;
            courier.IsAvailable = model.IsAvailable;

            _context.SaveChanges();

            return Ok(new { id = courier.Id });
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
