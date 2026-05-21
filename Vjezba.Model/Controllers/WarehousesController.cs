using Microsoft.AspNetCore.Mvc;
using Vjezba.Model.Data;

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
        [ValidateAntiForgeryToken]
        public IActionResult SoftDelete(int id)
        {
            var warehouse = _context.Warehouses.FirstOrDefault(x => x.Id == id);
            if (warehouse is null)
            {
                return NotFound();
            }

            warehouse.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return RedirectToAction("Index", "Home", new { selectedType = "warehouse" });
        }
    }
}
