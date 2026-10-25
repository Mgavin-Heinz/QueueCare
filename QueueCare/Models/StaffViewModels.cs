using System.ComponentModel.DataAnnotations;

namespace QueueCare.Models
{
    public class StaffRowViewModel
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";

        // Deactivated by an admin (a far-future lockout).
        public bool IsDeactivated { get; set; }

        // Temporarily locked after failed logins (NFR-09), clears on its own.
        public bool IsLockedOut { get; set; }
    }

    public class StaffCreateViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Role { get; set; } = "Receptionist";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }

    public class StaffResetPasswordViewModel
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";
    }
}