using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace QueueCare.Models
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public string UserId { get; set; } = string.Empty; 
        public IdentityUser User { get; set; } = null!; 
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;
        [MaxLength(100)]
        public string EntityName { get; set; } = string.Empty;
        [MaxLength(50)]
        public string? EntityId { get; set; } 
        public DateTime Timestamp { get; set; }
    }
}
