using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueueCare.Data;
using QueueCare.Models;

namespace QueueCare.Controllers
{
    [Authorize(Roles = "Patient")]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AppointmentsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Appointments/Book
        public async Task<IActionResult> Book()
        {
            await LoadClinicDaysAsync();
            return View(new BookAppointmentViewModel { SlotTime = new TimeOnly(8, 0) });
        }

        // POST: /Appointments/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookAppointmentViewModel model)
        {
            await LoadClinicDaysAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var patient = await GetCurrentPatientAsync();
            if (patient == null)
            {
                return Fail(model, "No patient profile was found for your account. Please contact reception.");
            }

            var day = await _db.ClinicDays
                .Include(c => c.Service)
                .FirstOrDefaultAsync(c => c.ClinicDayId == model.ClinicDayId);

            if (day == null || !day.Service.IsActive)
            {
                return Fail(model, "That clinic day is not available.");
            }

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (day.Date < today)
            {
                return Fail(model, "You cannot book a day that has already passed.");
            }

            if (model.SlotTime < day.OpenTime || model.SlotTime >= day.CloseTime)
            {
                return Fail(model, $"Please choose a time between {day.OpenTime:HH\\:mm} and {day.CloseTime:HH\\:mm}.");
            }

            if (model.SlotTime.Minute % 15 != 0)
            {
                return Fail(model, "Please choose a time on the quarter hour (for example 08:00, 08:15, 08:30).");
            }

            if (day.Date == today && model.SlotTime <= TimeOnly.FromDateTime(DateTime.Now))
            {
                return Fail(model, "That time has already passed today.");
            }

            var alreadyBooked = await _db.Appointments.AnyAsync(a =>
                a.PatientId == patient.PatientId &&
                a.ClinicDayId == day.ClinicDayId &&
                a.Status != AppointmentStatus.Cancelled);

            if (alreadyBooked)
            {
                return Fail(model, "You already have an appointment for this service on that day.");
            }

            var bookedCount = await _db.Appointments.CountAsync(a =>
                a.ClinicDayId == day.ClinicDayId &&
                a.Status != AppointmentStatus.Cancelled);

            if (bookedCount >= day.Capacity)
            {
                return Fail(model, "Sorry, that day is fully booked. Please choose another day.");
            }

            _db.Appointments.Add(new Appointment
            {
                PatientId = patient.PatientId,
                ClinicDayId = day.ClinicDayId,
                SlotTime = model.SlotTime,
                Status = AppointmentStatus.Booked,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Message"] = "Your appointment has been booked.";
            return RedirectToAction(nameof(Mine));
        }

        // GET: /Appointments/Mine
        public async Task<IActionResult> Mine()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null)
            {
                return View(new List<Appointment>());
            }

            var appointments = await _db.Appointments
                .Include(a => a.ClinicDay)
                    .ThenInclude(c => c.Service)
                .Where(a => a.PatientId == patient.PatientId)
                .OrderByDescending(a => a.ClinicDay.Date)
                .ThenBy(a => a.SlotTime)
                .ToListAsync();

            return View(appointments);
        }

        // POST: /Appointments/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null)
            {
                return NotFound();
            }

            // Only the patient's own booked appointments can be cancelled
            var appointment = await _db.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == id
                                       && a.PatientId == patient.PatientId
                                       && a.Status == AppointmentStatus.Booked);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = AppointmentStatus.Cancelled;
            await _db.SaveChangesAsync();

            TempData["Message"] = "Your appointment has been cancelled.";
            return RedirectToAction(nameof(Mine));
        }

        // ---------- helpers ----------

        private async Task LoadClinicDaysAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            ViewBag.ClinicDays = await _db.ClinicDays
                .Include(c => c.Service)
                .Where(c => c.Date >= today && c.Service.IsActive)
                .OrderBy(c => c.Date)
                .ThenBy(c => c.Service.Name)
                .ToListAsync();
        }

        private async Task<Patient?> GetCurrentPatientAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _db.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
        }

        private IActionResult Fail(BookAppointmentViewModel model, string message)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }
    }
}