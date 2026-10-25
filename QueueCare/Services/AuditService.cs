using QueueCare.Data;
using QueueCare.Models;

namespace QueueCare.Services
{
    // FR-51: one place to write audit log entries (who did what, and when).
    public class AuditService
    {
        private readonly ApplicationDbContext _db;

        public AuditService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(string? userId, string action, string entityName, string? entityId = null)
        {
            // AuditLog needs a user. Pages that call this are behind a login, so this is just a safety net.
            if (string.IsNullOrEmpty(userId)) return;

            _db.AuditLog.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}