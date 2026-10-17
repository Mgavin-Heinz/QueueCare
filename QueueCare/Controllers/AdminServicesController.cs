using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueCare.Data;
using QueueCare.Models;
using QueueCare.ViewModels;

namespace QueueCare.Controllers
{
    // Admin: manage services (requirement FR-46).
    // Only users in the Admin role may use this controller (roles come from Heinz's Program.cs / IdentitySeeder).
    [Authorize(Roles = "Admin")]
    public class AdminServicesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminServicesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /AdminServices
        public async Task<IActionResult> Index()
        {
            List<Service> services = await _db.Services
                .AsNoTracking()
                .OrderByDescending(s => s.IsActive)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return View(services);
        }

        // GET /AdminServices/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View("Form", new ServiceFormViewModel());
        }

        // POST /AdminServices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceFormViewModel model)
        {
            Normalise(model);
            await CheckForDuplicatesAsync(model, null);

            if (!ModelState.IsValid)
            {
                return View("Form", model);
            }

            var service = new Service
            {
                Name = model.Name,
                Code = model.Code,
                AvgConsultMinutes = model.AvgConsultMinutes!.Value,
                IsActive = model.IsActive
            };

            _db.Services.Add(service);
            await _db.SaveChangesAsync();
            await WriteAuditAsync("Created service", service.ServiceId);

            TempData["Message"] = $"Service '{service.Name}' was added.";
            return RedirectToAction(nameof(Index));
        }

        // GET /AdminServices/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            Service? service = await _db.Services.AsNoTracking().FirstOrDefaultAsync(s => s.ServiceId == id);
            if (service is null)
            {
                return NotFound();
            }

            var model = new ServiceFormViewModel
            {
                ServiceId = service.ServiceId,
                Name = service.Name,
                Code = service.Code,
                AvgConsultMinutes = service.AvgConsultMinutes,
                IsActive = service.IsActive
            };

            return View("Form", model);
        }

        // POST /AdminServices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceFormViewModel model)
        {
            if (id != model.ServiceId)
            {
                return BadRequest();
            }

            Service? service = await _db.Services.FirstOrDefaultAsync(s => s.ServiceId == id);
            if (service is null)
            {
                return NotFound();
            }

            Normalise(model);
            await CheckForDuplicatesAsync(model, id);

            if (!ModelState.IsValid)
            {
                return View("Form", model);
            }

            service.Name = model.Name;
            service.Code = model.Code;
            service.AvgConsultMinutes = model.AvgConsultMinutes!.Value;
            service.IsActive = model.IsActive;

            await _db.SaveChangesAsync();
            await WriteAuditAsync("Edited service", service.ServiceId);

            TempData["Message"] = $"Service '{service.Name}' was saved.";
            return RedirectToAction(nameof(Index));
        }

        // POST /AdminServices/Deactivate/5
        // Services are never deleted: old appointments and reports still point to them.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Deactivate(int id) => SetActiveAsync(id, false);

        // POST /AdminServices/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Activate(int id) => SetActiveAsync(id, true);

        private async Task<IActionResult> SetActiveAsync(int id, bool active)
        {
            Service? service = await _db.Services.FirstOrDefaultAsync(s => s.ServiceId == id);
            if (service is null)
            {
                return NotFound();
            }

            service.IsActive = active;
            await _db.SaveChangesAsync();
            await WriteAuditAsync(active ? "Activated service" : "Deactivated service", service.ServiceId);

            TempData["Message"] = active
                ? $"Service '{service.Name}' is active again."
                : $"Service '{service.Name}' was deactivated. Patients can no longer book it.";
            return RedirectToAction(nameof(Index));
        }

        private static void Normalise(ServiceFormViewModel model)
        {
            model.Name = (model.Name ?? string.Empty).Trim();
            model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        }

        // The ticket code must be unique (FR-46). SQL Server compares text without
        // caring about upper or lower case, so "c" and "C" count as the same code.
        private async Task CheckForDuplicatesAsync(ServiceFormViewModel model, int? ignoreServiceId)
        {
            if (model.Code.Length > 0 &&
                await _db.Services.AnyAsync(s => s.Code == model.Code && s.ServiceId != ignoreServiceId))
            {
                ModelState.AddModelError(nameof(model.Code), "This ticket code is already used by another service.");
            }

            if (model.Name.Length > 0 &&
                await _db.Services.AnyAsync(s => s.Name == model.Name && s.ServiceId != ignoreServiceId))
            {
                ModelState.AddModelError(nameof(model.Name), "A service with this name already exists.");
            }
        }

        // FR-50: record who changed what and when.
        // AuditLog needs a user. The Admin check means someone is always signed in here;
        // the null check is only a safety net.
        private async Task WriteAuditAsync(string action, int serviceId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
            {
                return;
            }

            _db.AuditLog.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = nameof(Service),
                EntityId = serviceId.ToString(),
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }
    }
}
