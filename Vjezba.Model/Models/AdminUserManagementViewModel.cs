using System.ComponentModel.DataAnnotations;

namespace Vjezba.Model.Models
{
    public class AdminUserManagementViewModel
    {
        public AdminCreateUserViewModel CreateUser { get; set; } = new();
        public List<AdminUserRowViewModel> Users { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();
        public string? StatusMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AdminCreateUserViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "Password must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "OIB must contain exactly 11 digits.")]
        public string OIB { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "JMBG must contain exactly 13 digits.")]
        public string JMBG { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Basic";

        public bool IsActive { get; set; } = true;
    }

    public class AdminUserRowViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string OIB { get; set; } = string.Empty;
        public string JMBG { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsCurrentUser { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public List<string> Roles { get; set; } = new();
        public string PrimaryRole { get; set; } = "Basic";
    }
}
