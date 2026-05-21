using Microsoft.AspNetCore.Mvc;
using Vjezba.Model.Data;

namespace Vjezba.Model.Controllers
{
    [Route("status-logs")]
    public class StatusLogsController : Controller
    {
        private readonly AppDbContext _context;

        public StatusLogsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{id:int}/soft-delete")]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDelete(int id)
        {
            var statusLog = _context.StatusLogs.FirstOrDefault(x => x.Id == id);
            if (statusLog is null)
            {
                return NotFound();
            }

            statusLog.DeletedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return RedirectToAction("Index", "Home", new { selectedType = "statuslog" });
        }
    }
}
