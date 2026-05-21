using Microsoft.AspNetCore.Mvc;
using Vjezba.Model.Data;

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
    }
}
