using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace QueueCare.Models
{
    public class Patient
    {
        public int PatientId { get; set; }
        public string? UserId { get; set; }
        public IdentityUser? User { get; set; } 
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        [MaxLength(13)]
        public string? IdNumber { get; set; }
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty; 
        public DateTime CreatedAt { get; set; }
    }
}
