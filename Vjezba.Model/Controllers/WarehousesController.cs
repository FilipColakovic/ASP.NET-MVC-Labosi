using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vjezba.Model.Data;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers
{
    [Route("warehouses")]
    public class WarehousesController : Controller
    {
        private readonly AppDbContext _context;

        public WarehousesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{id:int}/soft-delete")]
        [Authorize(Roles = "Admin")]
        public IActionResult SoftDelete(int id)
        {
            var warehouse = _context.Warehouses.FirstOrDefault(x => x.Id == id);
            if (warehouse is null)
            {
                return NotFound();
            }

            warehouse.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return RedirectToAction("Manifest", "Home", new { selectedType = "warehouse" });
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create(WarehouseCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var address = _context.Addresses.FirstOrDefault(x => x.Id == model.AddressId);
            if (address is null)
            {
                ModelState.AddModelError(nameof(WarehouseCreateViewModel.AddressId), "Address not found.");
                return BadRequest(new { errors = BuildErrors() });
            }

            var warehouse = new Warehouse
            {
                Name = model.Name.Trim(),
                AddressId = model.AddressId,
                Capacity = model.Capacity
            };

            _context.Warehouses.Add(warehouse);
            _context.SaveChanges();

            return Ok(new { id = warehouse.Id });
        }

        [HttpPost("edit")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Edit(WarehouseEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var warehouse = _context.Warehouses.FirstOrDefault(x => x.Id == model.Id);
            if (warehouse is null)
            {
                return NotFound();
            }

            var address = _context.Addresses.FirstOrDefault(x => x.Id == model.AddressId);
            if (address is null)
            {
                ModelState.AddModelError(nameof(WarehouseEditViewModel.AddressId), "Address not found.");
                return BadRequest(new { errors = BuildErrors() });
            }

            warehouse.Name = model.Name.Trim();
            warehouse.AddressId = model.AddressId;
            warehouse.Capacity = model.Capacity;

            _context.SaveChanges();

            return Ok(new { id = warehouse.Id });
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
