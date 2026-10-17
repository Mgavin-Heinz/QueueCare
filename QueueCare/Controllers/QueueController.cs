using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueCare.Models;

namespace QueueCare.Controllers
{
    [Authorize(Roles = "Patient")]
    public class QueueController : Controller
    {
        // TODO: Replace with real QueueService logic once Heinz builds it.
        public IActionResult Live()
        {
            // Mock data matching your wireframe
            var model = new LiveQueueViewModel
            {
                Position = 4,
                EstimatedWaitMinutes = 25,
                NowServingTicket = "C-010",
                YourTicket = "C-014",
                CurrentStage = 1 // Waiting
            };

            return View(model);
        }
    }
}