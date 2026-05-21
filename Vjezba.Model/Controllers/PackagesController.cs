using Microsoft.AspNetCore.Mvc;
using Vjezba.Model.Data;

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
        [ValidateAntiForgeryToken]
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
    }
}
