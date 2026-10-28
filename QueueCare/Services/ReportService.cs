using Microsoft.EntityFrameworkCore;
using QueueCare.Data;
using QueueCare.Models;

namespace QueueCare.Services
{
    public class ReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReportViewModel> GetReportAsync(DateOnly from, DateOnly to, int? serviceId)
        {

            var report = new ReportViewModel
            {
                FromDate = from,
                ToDate = to,
                ServiceId = serviceId,
                Services = await _context.Services
               .Where(s => s.IsActive)
               .OrderBy(s => s.Name)
               .ToListAsync()
            };

            var entries = _context.QueueEntries
                .Where(q => q.ClinicDay.Date >= from && q.ClinicDay.Date <= to);
            var appointments = _context.Appointments
                .Where(a => a.ClinicDay.Date >= from && a.ClinicDay.Date <= to);

            if (serviceId != null)
            {
                entries = entries.Where(q => q.ClinicDay.ServiceId == serviceId);
                appointments = appointments.Where(a => a.ClinicDay.ServiceId == serviceId);
            }


            report.ServedPerDay = await entries
                .Where(q => q.Status == QueueStatus.Served)
                .GroupBy(q => q.ClinicDay.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyServedCount { Date = g.Key, Count = g.Count() })
                .ToListAsync();


            var waits = await entries
                .Where(q => q.CalledAt != null)
                .Select(q => new { q.CheckedInAt, CalledAt = q.CalledAt!.Value })
                .ToListAsync();

            if (waits.Any())
            {
                report.AverageWaitMinutes = waits.Average(w => (w.CalledAt - w.CheckedInAt).TotalMinutes);
            }


            var finished = appointments
            .Where(a => a.Status == AppointmentStatus.CheckedIn ||
                 a.Status == AppointmentStatus.Completed ||
                 a.Status == AppointmentStatus.NoShow);

            var total = await finished.CountAsync();
            var noShows = await finished.CountAsync(a => a.Status == AppointmentStatus.NoShow);

            if (total > 0)
            {
                report.NoShowRatePercent = noShows * 100.0 / total;
            }
            return report;
        }
    }
}