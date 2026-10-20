using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueCare.Services;

namespace QueueCare.Controllers
{
    [Authorize(Roles = "Patient")]
    public class QueueController : Controller
    {
        private readonly QueueService _queue;

        public QueueController(QueueService queue)
        {
            _queue = queue;
        }

        // GET: /Queue/Live
        public async Task<IActionResult> Live()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var model = userId == null ? null : await _queue.GetLiveQueueAsync(userId);

            if (model == null)
            {
                TempData["Message"] = "You are not in today's queue yet. Check in at reception when you arrive.";
                return RedirectToAction("Mine", "Appointments");
            }

            return View(model);
        }
    }
}