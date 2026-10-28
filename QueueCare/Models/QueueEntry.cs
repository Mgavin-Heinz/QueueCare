using System.ComponentModel.DataAnnotations;

namespace QueueCare.Models
{
    public class QueueEntry
    {
        public int QueueEntryId { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public int ClinicDayId { get; set; }
        public ClinicDay ClinicDay { get; set; } = null!; 
        public int? AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }
        [MaxLength(10)]
        public string TicketNumber { get; set; } = string.Empty; 
        public QueueStatus Status { get; set; }
        public DateTime CheckedInAt { get; set; }
        public DateTime? CalledAt { get; set; }
        public DateTime? ServedAt { get; set; }
    }

    public enum QueueStatus
    {
        Waiting, 
        Called, 
        InConsultation, 
        Served, 
        NoShow
    }
}
