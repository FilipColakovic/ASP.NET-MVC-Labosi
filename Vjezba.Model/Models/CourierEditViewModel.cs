using System.ComponentModel.DataAnnotations;

namespace Vjezba.Model.Models
{
    public class CourierEditViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string VehicleType { get; set; } = string.Empty;

        [Required]
        public string LicensePlate { get; set; } = string.Empty;

        public bool IsAvailable { get; set; }
    }
}
