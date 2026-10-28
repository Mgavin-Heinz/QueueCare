using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueCare.Data;
using QueueCare.Services;

namespace QueueCare.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, int? serviceId)
        {
            var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);
            var fromDate = from ?? toDate.AddDays(-6);

            if (fromDate > toDate)
            {
                (fromDate, toDate) = (toDate, fromDate);
            }

            var service = new ReportService(_context);
            var report = await service.GetReportAsync(fromDate, toDate, serviceId);
            return View(report);
        }
    }
}