using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueCare.Models;

namespace QueueCare.Controllers
{
    [Authorize(Roles = "Receptionist")]
    public class ReceptionController : Controller
    {
        // TODO: Replace with real DB calls once Heinz builds the real logic.
        public IActionResult Dashboard()
        {
            var model = new ReceptionDashboardViewModel
            {
                WaitingCount = 12,
                ServedTodayCount = 34,
                NoShowCount = 2,
                Queue = new List<QueueItemViewModel>
                {
                    new() { QueueEntryId = 1, TicketNumber = "C-014", PatientName = "John Doe", ServiceName = "Chronic Meds", Status = "Called" },
                    new() { QueueEntryId = 2, TicketNumber = "C-015", PatientName = "Jane Smith", ServiceName = "General", Status = "Waiting" },
                    new() { QueueEntryId = 3, TicketNumber = "C-016", PatientName = "Peter X", ServiceName = "Child Health", Status = "Waiting" },
                    new() { QueueEntryId = 4, TicketNumber = "C-017", PatientName = "Mary Jane", ServiceName = "Family Planning", Status = "Waiting" }
                }
            };

            return View(model);
        }

        // ---------- MOCK actions: wire these to the real queue logic ----------

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CallNext(int id) => Flash($"Called queue entry {id}.");

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Served(int id) => Flash($"Marked queue entry {id} as served.");

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult NoShow(int id) => Flash($"Marked queue entry {id} as a no-show.");

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Skip(int id) => Flash($"Skipped queue entry {id}.");

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CheckIn(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Flash("Enter an ID or phone number to check someone in.");

            // TODO: look the patient up by ID / phone, or start a walk-in registration
            return Flash($"Check-in for \"{query.Trim()}\" will be wired to the real logic.");
        }

        private IActionResult Flash(string message)
        {
            TempData["Message"] = message;
            return RedirectToAction(nameof(Dashboard));
        }
    }
}