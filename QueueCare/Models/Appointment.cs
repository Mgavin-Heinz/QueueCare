namespace QueueCare.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public int ClinicDayId { get; set; }
        public ClinicDay ClinicDay { get; set; } = null!;
        public TimeOnly SlotTime { get; set; }
        public AppointmentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum AppointmentStatus
    {
        Booked, 
        Cancelled, 
        CheckedIn, 
        NoShow, 
        Completed
    }
}
