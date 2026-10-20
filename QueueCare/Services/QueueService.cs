using Microsoft.EntityFrameworkCore;
using QueueCare.Data;
using QueueCare.Models;
using QueueCare.Validation;

namespace QueueCare.Services
{
    /// <summary>What happened when someone clicked a button. Message is shown to the user.</summary>
    public class QueueResult
    {
        public bool Ok { get; init; }
        public string Message { get; init; } = string.Empty;

        // Set when the search found nobody (or someone with no booking today) and the
        // receptionist should add them as a walk-in instead.
        public bool NeedsWalkIn { get; init; }
        public int? PatientId { get; init; }

        public static QueueResult Success(string message) => new() { Ok = true, Message = message };
        public static QueueResult Fail(string message) => new() { Ok = false, Message = message };
    }

    /// <summary>
    /// All the queue rules in one place: check-in, walk-ins, calling, consultations,
    /// the reception / nurse / patient views. The controllers only call this.
    /// </summary>
    public class QueueService
    {
        private readonly ApplicationDbContext _db;

        public QueueService(ApplicationDbContext db)
        {
            _db = db;
        }

        private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Today);

        // ---------------------------------------------------------------
        // Capacity (BR-01): booked appointments and walk-ins share the same number
        // ---------------------------------------------------------------

        public async Task<int> CountUsedAsync(int clinicDayId)
        {
            int booked = await _db.Appointments
                .CountAsync(a => a.ClinicDayId == clinicDayId && a.Status != AppointmentStatus.Cancelled);
            int walkIns = await _db.QueueEntries
                .CountAsync(q => q.ClinicDayId == clinicDayId && q.AppointmentId == null);
            return booked + walkIns;
        }

        // ---------------------------------------------------------------
        // Reception
        // ---------------------------------------------------------------

        public async Task<ReceptionDashboardViewModel> GetDashboardAsync()
        {
            var today = Today();

            var entries = await _db.QueueEntries
                .Include(q => q.Patient)
                .Include(q => q.ClinicDay).ThenInclude(c => c.Service)
                .Where(q => q.ClinicDay.Date == today)
                .OrderBy(q => q.CheckedInAt)
                .ToListAsync();

            return new ReceptionDashboardViewModel
            {
                WaitingCount = entries.Count(q => q.Status == QueueStatus.Waiting),
                ServedTodayCount = entries.Count(q => q.Status == QueueStatus.Served),
                NoShowCount = entries.Count(q => q.Status == QueueStatus.NoShow),
                Queue = entries
                    .Where(q => q.Status == QueueStatus.Waiting
                             || q.Status == QueueStatus.Called
                             || q.Status == QueueStatus.InConsultation)
                    .Select(ToItem)
                    .ToList()
            };
        }

        /// <summary>
        /// Check someone in by ID number or cellphone number. If they have a booking for today
        /// they join the queue; otherwise the result tells the controller to open the walk-in form.
        /// </summary>
        public async Task<QueueResult> CheckInAsync(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return QueueResult.Fail("Enter an ID number or cellphone number to check someone in.");
            }

            var text = query.Trim();
            var phone = PhoneNumberValidator.Normalize(text);

            var matches = await _db.Patients
                .Where(p => p.IdNumber == text || (phone != null && p.PhoneNumber == phone))
                .ToListAsync();

            if (matches.Count == 0)
            {
                return new QueueResult
                {
                    Ok = false,
                    NeedsWalkIn = true,
                    Message = "No patient found with that number. Add them as a walk-in."
                };
            }

            if (matches.Count > 1)
            {
                return QueueResult.Fail("More than one patient uses that number. Search with their ID number instead.");
            }

            var patient = matches[0];
            var name = $"{patient.FirstName} {patient.LastName}";
            var today = Today();

            var existing = await _db.QueueEntries
                .Where(q => q.PatientId == patient.PatientId
                         && q.ClinicDay.Date == today
                         && (q.Status == QueueStatus.Waiting
                          || q.Status == QueueStatus.Called
                          || q.Status == QueueStatus.InConsultation))
                .FirstOrDefaultAsync();
            if (existing != null)
            {
                return QueueResult.Fail($"{name} is already in the queue (ticket {existing.TicketNumber}).");
            }

            var appointment = await _db.Appointments
                .Include(a => a.ClinicDay).ThenInclude(c => c.Service)
                .Where(a => a.PatientId == patient.PatientId
                         && a.ClinicDay.Date == today
                         && a.Status == AppointmentStatus.Booked)
                .OrderBy(a => a.SlotTime)
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return new QueueResult
                {
                    Ok = false,
                    NeedsWalkIn = true,
                    PatientId = patient.PatientId,
                    Message = $"{name} has no booking for today. Add them as a walk-in."
                };
            }

            var entry = AddToQueue(patient.PatientId, appointment.ClinicDay, appointment.AppointmentId, await NextNumberAsync(appointment.ClinicDay));
            appointment.Status = AppointmentStatus.CheckedIn;
            await _db.SaveChangesAsync();

            return QueueResult.Success($"{name} checked in. Ticket {entry.TicketNumber}.");
        }

        /// <summary>Add a patient with no booking to today's queue.</summary>
        public async Task<QueueResult> AddWalkInAsync(WalkInViewModel model)
        {
            var today = Today();

            var day = await _db.ClinicDays
                .Include(c => c.Service)
                .FirstOrDefaultAsync(c => c.ClinicDayId == model.ClinicDayId
                                       && c.Date == today
                                       && c.Service.IsActive);
            if (day == null)
            {
                return QueueResult.Fail("Choose one of today's services.");
            }

            if (await CountUsedAsync(day.ClinicDayId) >= day.Capacity)
            {
                return QueueResult.Fail($"{day.Service.Name} is full for today.");
            }

            // Reuse the patient if we already know them, otherwise create a record (no login).
            var idNumber = string.IsNullOrWhiteSpace(model.IdNumber) ? null : model.IdNumber.Trim();
            Patient? patient = null;
            if (model.PatientId != null)
            {
                patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == model.PatientId);
            }
            if (patient == null && idNumber != null)
            {
                patient = await _db.Patients.FirstOrDefaultAsync(p => p.IdNumber == idNumber);
            }
            if (patient == null)
            {
                patient = new Patient
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    IdNumber = idNumber,
                    PhoneNumber = PhoneNumberValidator.Normalize(model.PhoneNumber) ?? model.PhoneNumber.Trim(),
                    CreatedAt = DateTime.Now
                };
                _db.Patients.Add(patient);
                await _db.SaveChangesAsync();
            }

            var alreadyQueued = await _db.QueueEntries.AnyAsync(q =>
                q.PatientId == patient.PatientId
                && q.ClinicDayId == day.ClinicDayId
                && (q.Status == QueueStatus.Waiting
                 || q.Status == QueueStatus.Called
                 || q.Status == QueueStatus.InConsultation));
            if (alreadyQueued)
            {
                return QueueResult.Fail("That patient is already in today's queue for this service.");
            }

            var entry = AddToQueue(patient.PatientId, day, null, await NextNumberAsync(day));
            await _db.SaveChangesAsync();

            return QueueResult.Success($"Walk-in added. Ticket {entry.TicketNumber}.");
        }

        // ---------------------------------------------------------------
        // Calling and consultations
        // ---------------------------------------------------------------

        public async Task<QueueResult> CallNextAsync(int id)
        {
            var entry = await LoadAsync(id);
            if (entry == null) return QueueResult.Fail("That queue entry was not found.");
            if (entry.Status != QueueStatus.Waiting) return QueueResult.Fail("Only waiting patients can be called.");

            entry.Status = QueueStatus.Called;
            entry.CalledAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return QueueResult.Success($"Called {entry.TicketNumber}.");
        }

        public async Task<QueueResult> StartConsultationAsync(int id)
        {
            var entry = await LoadAsync(id);
            if (entry == null) return QueueResult.Fail("That queue entry was not found.");
            if (entry.Status != QueueStatus.Called) return QueueResult.Fail("Only a patient who has been called can start a consultation.");

            entry.Status = QueueStatus.InConsultation;
            await _db.SaveChangesAsync();
            return QueueResult.Success($"Consultation started for {entry.TicketNumber}.");
        }

        /// <summary>Used by the Served button (reception) and the Done button (nurse).</summary>
        public async Task<QueueResult> MarkServedAsync(int id)
        {
            var entry = await LoadAsync(id);
            if (entry == null) return QueueResult.Fail("That queue entry was not found.");
            if (entry.Status != QueueStatus.Called && entry.Status != QueueStatus.InConsultation)
            {
                return QueueResult.Fail("Only a patient who has been called can be marked as served.");
            }

            entry.Status = QueueStatus.Served;
            entry.ServedAt = DateTime.Now;
            entry.CalledAt ??= entry.ServedAt;
            if (entry.Appointment != null) entry.Appointment.Status = AppointmentStatus.Completed;
            await _db.SaveChangesAsync();
            return QueueResult.Success($"{entry.TicketNumber} marked as served.");
        }

        public async Task<QueueResult> MarkNoShowAsync(int id)
        {
            var entry = await LoadAsync(id);
            if (entry == null) return QueueResult.Fail("That queue entry was not found.");
            if (entry.Status != QueueStatus.Called) return QueueResult.Fail("Only a patient who has been called can be marked as a no-show.");

            entry.Status = QueueStatus.NoShow;
            if (entry.Appointment != null) entry.Appointment.Status = AppointmentStatus.NoShow;
            await _db.SaveChangesAsync();
            return QueueResult.Success($"{entry.TicketNumber} marked as a no-show.");
        }

        /// <summary>A called patient who didn't come forward goes back on the waiting list.</summary>
        public async Task<QueueResult> SkipAsync(int id)
        {
            var entry = await LoadAsync(id);
            if (entry == null) return QueueResult.Fail("That queue entry was not found.");
            if (entry.Status != QueueStatus.Called)
            {
                return QueueResult.Fail("Only a called patient can be skipped. Use Call Next on someone else instead.");
            }

            entry.Status = QueueStatus.Waiting;
            entry.CalledAt = null;
            await _db.SaveChangesAsync();
            return QueueResult.Success($"{entry.TicketNumber} is back on the waiting list.");
        }

        // ---------------------------------------------------------------
        // Nurse
        //// ---------------------------------------------------------------

        // TODO: uncomment after Tirick's PR #21 (NurseDashboardViewModel) is merged


        //public async Task<NurseDashboardViewModel> GetNurseDashboardAsync()
        //{
        //    var today = Today();

        //    var entries = await _db.QueueEntries
        //        .Include(q => q.Patient)
        //        .Include(q => q.ClinicDay).ThenInclude(c => c.Service)
        //        .Where(q => q.ClinicDay.Date == today
        //                 && (q.Status == QueueStatus.Waiting
        //                  || q.Status == QueueStatus.Called
        //                  || q.Status == QueueStatus.InConsultation))
        //        .OrderBy(q => q.CheckedInAt)
        //        .ToListAsync();

        //    // Someone already in consultation comes first, then someone who has been called.
        //    var current = entries.FirstOrDefault(q => q.Status == QueueStatus.InConsultation)
        //               ?? entries.FirstOrDefault(q => q.Status == QueueStatus.Called);

        //    return new NurseDashboardViewModel
        //    {
        //        CurrentPatient = current == null ? null : ToItem(current),
        //        UpcomingPatients = entries
        //            .Where(q => q.Status == QueueStatus.Waiting)
        //            .Select(ToItem)
        //            .ToList()
        //    };
        //}

        // ---------------------------------------------------------------
        // Patient: live queue
        // ---------------------------------------------------------------

        /// <summary>Returns null when the patient is not in today's queue.</summary>
        public async Task<LiveQueueViewModel?> GetLiveQueueAsync(string userId)
        {
            var today = Today();

            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            if (patient == null) return null;

            var entry = await _db.QueueEntries
                .Include(q => q.ClinicDay).ThenInclude(c => c.Service)
                .Where(q => q.PatientId == patient.PatientId
                         && q.ClinicDay.Date == today
                         && q.Status != QueueStatus.NoShow)
                .OrderByDescending(q => q.CheckedInAt)
                .FirstOrDefaultAsync();
            if (entry == null) return null;

            int ahead = 0;
            if (entry.Status == QueueStatus.Waiting)
            {
                ahead = await _db.QueueEntries.CountAsync(q =>
                    q.ClinicDayId == entry.ClinicDayId
                    && q.Status == QueueStatus.Waiting
                    && q.CheckedInAt < entry.CheckedInAt);
            }

            var nowServing = await _db.QueueEntries
                .Where(q => q.ClinicDayId == entry.ClinicDayId
                         && (q.Status == QueueStatus.Called || q.Status == QueueStatus.InConsultation))
                .OrderByDescending(q => q.CalledAt)
                .FirstOrDefaultAsync();

            return new LiveQueueViewModel
            {
                Position = entry.Status == QueueStatus.Waiting ? ahead + 1 : 1,
                EstimatedWaitMinutes = ahead * entry.ClinicDay.Service.AvgConsultMinutes,
                NowServingTicket = nowServing?.TicketNumber ?? "-",
                YourTicket = entry.TicketNumber,
                CurrentStage = entry.Status switch
                {
                    QueueStatus.Waiting => 1,
                    QueueStatus.Called => 2,
                    QueueStatus.InConsultation => 3,
                    _ => 4
                }
            };
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private Task<QueueEntry?> LoadAsync(int id) =>
            _db.QueueEntries
                .Include(q => q.Appointment)
                .FirstOrDefaultAsync(q => q.QueueEntryId == id);

        // Ticket numbers are the service code plus a running number for that day, e.g. G001, G002.
        // (Two receptionists checking in at the exact same moment could get the same number.)
        private async Task<int> NextNumberAsync(ClinicDay day) =>
            await _db.QueueEntries.CountAsync(q => q.ClinicDayId == day.ClinicDayId) + 1;

        private QueueEntry AddToQueue(int patientId, ClinicDay day, int? appointmentId, int number)
        {
            var entry = new QueueEntry
            {
                PatientId = patientId,
                ClinicDayId = day.ClinicDayId,
                AppointmentId = appointmentId,
                TicketNumber = $"{day.Service.Code}{number:000}",
                Status = QueueStatus.Waiting,
                CheckedInAt = DateTime.Now
            };
            _db.QueueEntries.Add(entry);
            return entry;
        }

        private static QueueItemViewModel ToItem(QueueEntry q) => new()
        {
            QueueEntryId = q.QueueEntryId,
            TicketNumber = q.TicketNumber,
            PatientName = $"{q.Patient.FirstName} {q.Patient.LastName}",
            ServiceName = q.ClinicDay.Service.Name,
            Status = q.Status.ToString()
        };
    }
}