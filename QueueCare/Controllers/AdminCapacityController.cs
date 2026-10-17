using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QueueCare.Data;
using QueueCare.Models;
using QueueCare.Validation;
using QueueCare.ViewModels;

namespace QueueCare.Controllers
{
    // Admin: daily capacity and opening hours per service (requirements FR-47 and FR-48).
    // Works on Christopher's ClinicDay model (Service, Date, OpenTime, CloseTime, Capacity).
    [Authorize(Roles = "Admin")]
    public class AdminCapacityController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminCapacityController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /AdminCapacity  (use ?showPast=true to include days that have passed)
        public async Task<IActionResult> Index(bool showPast = false)
        {
            DateOnly today = Today();

            IQueryable<ClinicDay> query = _db.ClinicDays.AsNoTracking();
            if (!showPast)
            {
                query = query.Where(c => c.Date >= today);
            }

            // Used = booked appointments that are not cancelled + walk-ins (queue entries with no appointment).
            List<CapacityRowViewModel> rows = await query
                .OrderBy(c => c.Date)
                .ThenBy(c => c.Service.Name)
                .Select(c => new CapacityRowViewModel
                {
                    ClinicDayId = c.ClinicDayId,
                    Date = c.Date,
                    ServiceName = c.Service.Name,
                    ServiceCode = c.Service.Code,
                    OpenTime = c.OpenTime,
                    CloseTime = c.CloseTime,
                    Capacity = c.Capacity,
                    Used = _db.Appointments.Count(a => a.ClinicDayId == c.ClinicDayId && a.Status != AppointmentStatus.Cancelled)
                         + _db.QueueEntries.Count(q => q.ClinicDayId == c.ClinicDayId && q.AppointmentId == null)
                })
                .ToListAsync();

            ViewData["ShowPast"] = showPast;
            return View(rows);
        }

        // GET /AdminCapacity/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ClinicDayFormViewModel
            {
                Date = Today().AddDays(1),
                OpenTime = new TimeOnly(8, 0),
                CloseTime = new TimeOnly(16, 0)
            };

            await FillServicesAsync(model);
            return View("Form", model);
        }

        // POST /AdminCapacity/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClinicDayFormViewModel model)
        {
            Check(nameof(model.Date), CapacityRules.CheckDate(model.Date, Today()));
            Check(nameof(model.CloseTime), CapacityRules.CheckHours(model.OpenTime, model.CloseTime));
            Check(nameof(model.Capacity), CapacityRules.CheckCapacity(model.Capacity, 0));

            Service? service = await _db.Services
                .FirstOrDefaultAsync(s => s.ServiceId == model.ServiceId && s.IsActive);
            if (service is null)
            {
                ModelState.AddModelError(nameof(model.ServiceId), "Choose an active service.");
            }

            // One clinic day per service per date (Christopher's unique index).
            if (ModelState.IsValid && model.Date is not null &&
                await _db.ClinicDays.AnyAsync(c => c.ServiceId == model.ServiceId && c.Date == model.Date.Value))
            {
                ModelState.AddModelError(nameof(model.Date),
                    "This service already has a clinic day on this date. Edit that day instead.");
            }

            if (!ModelState.IsValid)
            {
                await FillServicesAsync(model);
                return View("Form", model);
            }

            var day = new ClinicDay
            {
                ServiceId = service!.ServiceId,
                Date = model.Date!.Value,
                OpenTime = model.OpenTime!.Value,
                CloseTime = model.CloseTime!.Value,
                Capacity = model.Capacity!.Value
            };

            _db.ClinicDays.Add(day);
            await _db.SaveChangesAsync();
            await WriteAuditAsync("Created clinic day", day.ClinicDayId);

            TempData["Message"] = $"Clinic day for {service.Name} on {day.Date:ddd d MMM yyyy} was added.";
            return RedirectToAction(nameof(Index));
        }

        // GET /AdminCapacity/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ClinicDay? day = await _db.ClinicDays.AsNoTracking()
                .Include(c => c.Service)
                .FirstOrDefaultAsync(c => c.ClinicDayId == id);
            if (day is null)
            {
                return NotFound();
            }

            var model = new ClinicDayFormViewModel
            {
                ClinicDayId = day.ClinicDayId,
                ServiceId = day.ServiceId,
                ServiceName = day.Service.Name,
                Date = day.Date,
                OpenTime = day.OpenTime,
                CloseTime = day.CloseTime,
                Capacity = day.Capacity,
                Used = await CountUsedAsync(day.ClinicDayId)
            };

            return View("Form", model);
        }

        // POST /AdminCapacity/Edit/5
        // Only the hours and the capacity can change. The date and service stay fixed because
        // appointments are already attached to this clinic day.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClinicDayFormViewModel model)
        {
            if (id != model.ClinicDayId)
            {
                return BadRequest();
            }

            ClinicDay? day = await _db.ClinicDays
                .Include(c => c.Service)
                .FirstOrDefaultAsync(c => c.ClinicDayId == id);
            if (day is null)
            {
                return NotFound();
            }

            int used = await CountUsedAsync(id);

            Check(nameof(model.CloseTime), CapacityRules.CheckHours(model.OpenTime, model.CloseTime));
            Check(nameof(model.Capacity), CapacityRules.CheckCapacity(model.Capacity, used));

            // No existing appointment may end up outside the new opening hours.
            if (model.OpenTime is not null && model.CloseTime is not null &&
                ModelState.GetFieldValidationState(nameof(model.CloseTime)) != ModelValidationState.Invalid)
            {
                TimeOnly open = model.OpenTime.Value;
                TimeOnly close = model.CloseTime.Value;

                bool outside = await _db.Appointments.AnyAsync(a =>
                    a.ClinicDayId == id &&
                    a.Status != AppointmentStatus.Cancelled &&
                    (a.SlotTime < open || a.SlotTime >= close));

                if (outside)
                {
                    ModelState.AddModelError(nameof(model.OpenTime),
                        "Some booked appointments fall outside these hours. Choose hours that still cover them.");
                }
            }

            // The page shows these, but they are not posted back.
            model.ServiceId = day.ServiceId;
            model.ServiceName = day.Service.Name;
            model.Date = day.Date;
            model.Used = used;

            if (!ModelState.IsValid)
            {
                return View("Form", model);
            }

            day.OpenTime = model.OpenTime!.Value;
            day.CloseTime = model.CloseTime!.Value;
            day.Capacity = model.Capacity!.Value;

            await _db.SaveChangesAsync();
            await WriteAuditAsync("Edited clinic day", day.ClinicDayId);

            TempData["Message"] = $"Clinic day for {day.Service.Name} on {day.Date:ddd d MMM yyyy} was saved.";
            return RedirectToAction(nameof(Index));
        }

        private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Today);

        // Adds a rule's message to the form, unless the field already has an error (for example unreadable text).
        private void Check(string field, ValidationOutcome outcome)
        {
            if (!outcome.IsValid &&
                ModelState.GetFieldValidationState(field) != ModelValidationState.Invalid)
            {
                ModelState.AddModelError(field, outcome.ErrorMessage!);
            }
        }

        private async Task FillServicesAsync(ClinicDayFormViewModel model)
        {
            model.Services = await _db.Services
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem { Value = s.ServiceId.ToString(), Text = s.Name + " (" + s.Code + ")" })
                .ToListAsync();
        }

        // BR-01: capacity covers booked appointments and walk-ins together.
        private async Task<int> CountUsedAsync(int clinicDayId)
        {
            int booked = await _db.Appointments
                .CountAsync(a => a.ClinicDayId == clinicDayId && a.Status != AppointmentStatus.Cancelled);
            int walkIns = await _db.QueueEntries
                .CountAsync(q => q.ClinicDayId == clinicDayId && q.AppointmentId == null);
            return booked + walkIns;
        }

        // FR-50: record who changed what and when.
        private async Task WriteAuditAsync(string action, int clinicDayId)
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
                EntityName = nameof(ClinicDay),
                EntityId = clinicDayId.ToString(),
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }
    }
}
