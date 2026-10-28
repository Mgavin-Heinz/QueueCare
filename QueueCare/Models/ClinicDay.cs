

namespace QueueCare.Models
{
    public class ClinicDay
    {
        public int ClinicDayId { get; set; }
        public int ServiceId { get; set; }
        public Service Service { get; set; } = null!; 
        public DateOnly Date {  get; set; } 
        public TimeOnly OpenTime { get; set; } 
        public TimeOnly CloseTime { get; set; } 
        public int Capacity { get; set; }
    }
}

