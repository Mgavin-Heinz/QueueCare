using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueueCare.Services;

namespace QueueCare.Controllers
{
    [Authorize(Roles = "Nurse,Admin")]
    public class NurseController : Controller
    {
        private readonly QueueService _queue;
        private readonly AuditService _audit;

        public NurseController(QueueService queue, AuditService audit)
        {
            _queue = queue;
            _audit = audit;
        }

        // GET: /Nurse/Next
        public async Task<IActionResult> Next()
        {
            return View(await _queue.GetNurseDashboardAsync());
        }

        // POST: /Nurse/Start/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id)
        {
            var result = await _queue.StartConsultationAsync(id);
            if (result.Ok) await _audit.LogAsync(UserId(), "Started consultation", "QueueEntry", id.ToString());
            TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Next));
        }

        // POST: /Nurse/Done/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Done(int id)
        {
            var result = await _queue.MarkServedAsync(id);
            if (result.Ok) await _audit.LogAsync(UserId(), "Marked served", "QueueEntry", id.ToString());
            TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Next));
        }

        private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}