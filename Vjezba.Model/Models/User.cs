using System.ComponentModel.DataAnnotations;

namespace Vjezba.Model.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Package> SentPackages { get; set; } = new List<Package>();
        public virtual ICollection<Package> ReceivedPackages { get; set; } = new List<Package>();
    }
}
