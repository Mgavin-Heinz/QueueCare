using Microsoft.EntityFrameworkCore;
using QueueCare.Models;

namespace QueueCare.Data
{
    public static class ClinicSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            if (!await db.Services.AnyAsync())
            {
                db.Services.AddRange(
                    new Service { Name = "General consultation", Code = "G", AvgConsultMinutes = 15, IsActive = true },
                    new Service { Name = "Chronic medication", Code = "C", AvgConsultMinutes = 10, IsActive = true },
                    new Service { Name = "Child health", Code = "CH", AvgConsultMinutes = 20, IsActive = true });
                await db.SaveChangesAsync();
            }

            var services = await db.Services.Where(s => s.IsActive).ToListAsync();
            var today = DateOnly.FromDateTime(DateTime.Today);
            var lastDay = today.AddDays(13);

            var existing = await db.ClinicDays
                .Where(c => c.Date >= today && c.Date <= lastDay)
                .Select(c => new { c.Date, c.ServiceId })
                .ToListAsync();

            for (var date = today; date <= lastDay; date = date.AddDays(1))
            {
                foreach (var service in services)
                {
                    if (existing.Any(e => e.Date == date && e.ServiceId == service.ServiceId))
                        continue;

                    db.ClinicDays.Add(new ClinicDay
                    {
                        ServiceId = service.ServiceId,
                        Date = date,
                        OpenTime = new TimeOnly(7, 0),
                        CloseTime = new TimeOnly(15, 0),
                        Capacity = 30
                    });
                }
            }

            await db.SaveChangesAsync();
        }
    }
}