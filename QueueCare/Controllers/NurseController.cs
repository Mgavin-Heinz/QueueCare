using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueCare.Models;

namespace QueueCare.Controllers
{
    [Authorize(Roles = "Nurse")]
    public class NurseController : Controller
    {
        // TODO: Replace with real QueueService logic once it exists.
        public IActionResult Next()
        {
            var model = new NurseDashboardViewModel
            {
                CurrentPatient = new QueueItemViewModel
                {
                    QueueEntryId = 1,
                    TicketNumber = "C-014",
                    PatientName = "John Doe",
                    ServiceName = "Chronic Meds",
                    // Change this to "Called" to test the Start button
                    Status = "InConsultation"
                },
                UpcomingPatients = new List<QueueItemViewModel>
                {
                    new() { QueueEntryId = 2, TicketNumber = "C-015", PatientName = "Jane Smith", ServiceName = "General", Status = "Waiting" },
                    new() { QueueEntryId = 3, TicketNumber = "C-016", PatientName = "Peter X", ServiceName = "Child Health", Status = "Waiting" },
                    new() { QueueEntryId = 4, TicketNumber = "C-017", PatientName = "Mary Jane", ServiceName = "Family Planning", Status = "Waiting" }
                }
            };

            return View(model);
        }

        // POST: /Nurse/Done/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Done(int id)
        {
            // TODO: Mark the appointment as Served in the DB
            TempData["Message"] = $"Consultation for queue entry {id} marked as done.";
            return RedirectToAction(nameof(Next));
        }

        // POST: /Nurse/Start/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Start(int id)
        {
            // TODO: Mark the appointment as InConsultation in the DB
            TempData["Message"] = $"Consultation for queue entry {id} started.";
            return RedirectToAction(nameof(Next));
        }
    }
}