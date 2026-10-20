using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueCare.Data;
using QueueCare.Models;
using QueueCare.Services;

namespace QueueCare.Controllers
{
    [Authorize(Roles = "Receptionist,Admin")]
    public class ReceptionController : Controller
    {
        private readonly QueueService _queue;
        private readonly ApplicationDbContext _db;

        public ReceptionController(QueueService queue, ApplicationDbContext db)
        {
            _queue = queue;
            _db = db;
        }

        // GET: /Reception/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            return View(await _queue.GetDashboardAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CallNext(int id) => Flash(await _queue.CallNextAsync(id));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Served(int id) => Flash(await _queue.MarkServedAsync(id));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NoShow(int id) => Flash(await _queue.MarkNoShowAsync(id));

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Skip(int id) => Flash(await _queue.SkipAsync(id));

        // POST: /Reception/CheckIn  (search box: ID number or cellphone number)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(string? query)
        {
            var result = await _queue.CheckInAsync(query);

            if (result.NeedsWalkIn)
            {
                TempData["Message"] = result.Message;
                return RedirectToAction(nameof(WalkIn), new { patientId = result.PatientId, query });
            }

            return Flash(result);
        }

        // GET: /Reception/WalkIn
        public async Task<IActionResult> WalkIn(int? patientId, string? query)
        {
            var model = new WalkInViewModel();

            if (patientId != null)
            {
                var patient = await _db.Patients.FindAsync(patientId);
                if (patient != null)
                {
                    model.PatientId = patient.PatientId;
                    model.FirstName = patient.FirstName;
                    model.LastName = patient.LastName;
                    model.IdNumber = patient.IdNumber;
                    model.PhoneNumber = patient.PhoneNumber;
                }
            }
            else if (!string.IsNullOrWhiteSpace(query))
            {
                // Pre-fill whatever they typed into the search box.
                var text = query.Trim();
                if (text.Length == 13) model.IdNumber = text; else model.PhoneNumber = text;
            }

            await LoadTodayClinicDaysAsync();
            return View(model);
        }

        // POST: /Reception/WalkIn
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> WalkIn(WalkInViewModel model)
        {
            await LoadTodayClinicDaysAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _queue.AddWalkInAsync(model);
            if (!result.Ok)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            return Flash(result);
        }

        // The dashboard shows TempData["Message"] as a toast, for successes and problems alike.
        private IActionResult Flash(QueueResult result)
        {
            TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Dashboard));
        }

        private async Task LoadTodayClinicDaysAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            ViewBag.ClinicDays = await _db.ClinicDays
                .Include(c => c.Service)
                .Where(c => c.Date == today && c.Service.IsActive)
                .OrderBy(c => c.Service.Name)
                .ToListAsync();
        }
    }
}