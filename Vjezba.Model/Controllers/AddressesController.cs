using Microsoft.AspNetCore.Mvc;
using Vjezba.Model.Data;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers
{
    [Route("addresses")]
    public class AddressesController : Controller
    {
        private readonly AppDbContext _context;

        public AddressesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{id:int}/soft-delete")]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDelete(int id)
        {
            var address = _context.Addresses.FirstOrDefault(x => x.Id == id);
            if (address is null)
            {
                return NotFound();
            }

            address.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return RedirectToAction("Index", "Home", new { selectedType = "address" });
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(AddressCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var address = new Address
            {
                Street = model.Street.Trim(),
                City = model.City.Trim(),
                PostalCode = model.PostalCode.Trim(),
                Country = model.Country.Trim()
            };

            _context.Addresses.Add(address);
            _context.SaveChanges();

            return Ok(new { id = address.Id });
        }

        [HttpPost("edit")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(AddressEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var address = _context.Addresses.FirstOrDefault(x => x.Id == model.Id);
            if (address is null)
            {
                return NotFound();
            }

            address.Street = model.Street.Trim();
            address.City = model.City.Trim();
            address.PostalCode = model.PostalCode.Trim();
            address.Country = model.Country.Trim();

            _context.SaveChanges();

            return Ok(new { id = address.Id });
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
