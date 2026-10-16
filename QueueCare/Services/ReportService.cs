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

        public async Task<ReportViewModel> GetReportAsync()
        {
            var from = DateOnly.FromDateTime(DateTime.Today).AddDays(-6);
            var report = new ReportViewModel();

            // 1. Patients served per day
            report.ServedPerDay = await _context.QueueEntries
                .Where(q => q.Status == QueueStatus.Served && q.ClinicDay.Date >= from)
                .GroupBy(q => q.ClinicDay.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyServedCount { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            // 2. Average wait it is calculated by the last 7 days where i take the checked in at data and the called at date 
            // and then take that and workout the average of that 
            var waits = await _context.QueueEntries
             .Where(q => q.CalledAt != null && q.ClinicDay.Date >= from)
             .Select(q => new { q.CheckedInAt, CalledAt = q.CalledAt!.Value })
             .ToListAsync();

            if (waits.Any())
            {
                report.AverageWaitMinutes = waits.Average(w => (w.CalledAt - w.CheckedInAt).TotalMinutes);
            }
            // 3. No-show rate
            // It counts the last 7 days of appointments where the day has already happened,
            // then divides the no-shows by that total and multiplies by 100 to get a percentage. To get the percentage of the noshows 
            // of the clinic 
            var finished = _context.Appointments
                .Where(a => a.ClinicDay.Date >= from &&
                           (a.Status == AppointmentStatus.CheckedIn ||
                            a.Status == AppointmentStatus.Completed ||
                            a.Status == AppointmentStatus.NoShow));

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