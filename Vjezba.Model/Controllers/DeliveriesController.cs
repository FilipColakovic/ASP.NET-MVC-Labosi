using Microsoft.AspNetCore.Mvc;
using Vjezba.Model.Data;

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
    }
}
