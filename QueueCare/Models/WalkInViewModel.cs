using System.ComponentModel.DataAnnotations;
using QueueCare.Validation;

namespace QueueCare.Models
{
    public class WalkInViewModel
    {
        // Set when the search already found the patient, so we reuse their record.
        public int? PatientId { get; set; }

        [Required(ErrorMessage = "Choose a service.")]
        [Display(Name = "Service")]
        public int? ClinicDayId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [SaIdNumber, MaxLength(13)]
        [Display(Name = "ID number (optional)")]
        public string? IdNumber { get; set; }

        [Required, SaPhoneNumber, MaxLength(20)]
        [Display(Name = "Cellphone number")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}