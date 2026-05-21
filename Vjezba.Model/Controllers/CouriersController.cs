using Microsoft.AspNetCore.Mvc;
using Vjezba.Model.Data;

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
        [ValidateAntiForgeryToken]
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
    }
}
