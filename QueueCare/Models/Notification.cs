using System.ComponentModel.DataAnnotations;

namespace QueueCare.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!; 
        public int? QueueEntryId { get; set; }
        public QueueEntry? QueueEntry { get; set; }
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
