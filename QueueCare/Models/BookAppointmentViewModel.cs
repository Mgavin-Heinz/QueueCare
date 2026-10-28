using System.ComponentModel.DataAnnotations;

namespace QueueCare.Models
{
    public class BookAppointmentViewModel
    {
        [Required(ErrorMessage = "Please choose a clinic day.")]
        [Display(Name = "Clinic day")]
        public int ClinicDayId { get; set; }

        [Required(ErrorMessage = "Please choose a time.")]
        [Display(Name = "Time")]
        public TimeOnly SlotTime { get; set; }
    }
}
