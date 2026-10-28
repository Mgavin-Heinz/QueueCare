using System.ComponentModel.DataAnnotations;

namespace QueueCare.Models
{
    public class Service
    {
        public int ServiceId { get; set; }
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(5)]
        public string Code { get; set; } = string.Empty; 
        public int AvgConsultMinutes { get; set; }
        public bool IsActive { get; set; }
    }
}
