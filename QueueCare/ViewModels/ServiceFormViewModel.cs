using System.ComponentModel.DataAnnotations;

namespace QueueCare.ViewModels
{
    /// <summary>
    /// The add / edit form for a service (requirement FR-46).
    /// A separate class is used so that Christopher's Service model does not have to change.
    /// </summary>
    public class ServiceFormViewModel
    {
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Enter the service name.")]
        [StringLength(100, ErrorMessage = "The name can be at most 100 characters.")]
        [Display(Name = "Service name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a ticket code, for example C.")]
        [StringLength(5, ErrorMessage = "The code can be at most 5 characters.")]
        [RegularExpression(@"^\s*[A-Za-z]+\s*$", ErrorMessage = "The code can only contain letters.")]
        [Display(Name = "Ticket code")]
        public string Code { get; set; } = string.Empty;

        // Nullable so that an empty box shows the "Required" message instead of silently becoming 0.
        [Required(ErrorMessage = "Enter the average consultation time in minutes.")]
        [Range(1, 1000, ErrorMessage = "Enter a whole number greater than 0.")]
        [Display(Name = "Average consultation (minutes)")]
        public int? AvgConsultMinutes { get; set; }

        [Display(Name = "Active (patients can book this service)")]
        public bool IsActive { get; set; } = true;
    }
}
