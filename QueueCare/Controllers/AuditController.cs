using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueCare.Data;

namespace QueueCare.Controllers
{
    // FR-51: admin-only page listing audit entries, newest first.
    [Authorize(Roles = "Admin")]
    public class AuditController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AuditController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var entries = await _db.AuditLog
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Take(200)
                .ToListAsync();
            return View(entries);
        }
    }
}