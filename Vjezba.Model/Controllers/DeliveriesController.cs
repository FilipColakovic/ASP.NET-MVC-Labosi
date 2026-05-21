using Microsoft.AspNetCore.Mvc;
using Vjezba.Model.Data;
using Vjezba.Model.Models;

namespace Vjezba.Model.Controllers
{
    [Route("deliveries")]
    public class DeliveriesController : Controller
    {
        private readonly AppDbContext _context;

        public DeliveriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{id:int}/soft-delete")]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDelete(int id)
        {
            var delivery = _context.Deliveries.FirstOrDefault(x => x.Id == id);
            if (delivery is null)
            {
                return NotFound();
            }

            delivery.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return RedirectToAction("Index", "Home", new { selectedType = "delivery" });
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(DeliveryCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = BuildErrors() });
            }

            var courier = _context.Couriers.FirstOrDefault(x => x.Id == model.CourierId);
            if (courier is null)
            {
                ModelState.AddModelError(nameof(DeliveryCreateViewModel.CourierId), "Courier not found.");
                return BadRequest(new { errors = BuildErrors() });
            }

            var delivery = new Delivery
            {
                DepartureDate = model.DepartureDate,
                ArrivalDate = model.ArrivalDate,
                CurrentLocation = model.CurrentLocation.Trim(),
                IsDelayed = model.IsDelayed,
                CourierId = model.CourierId
            };

            _context.Deliveries.Add(delivery);
            _context.SaveChanges();

            return Ok(new { id = delivery.Id });
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
