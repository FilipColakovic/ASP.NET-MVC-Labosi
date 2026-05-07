using System.ComponentModel.DataAnnotations;

namespace Vjezba.Model.Models
{
    public class Address
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Street { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        public string Country { get; set; } = string.Empty;

        public virtual ICollection<Package> SentPackages { get; set; } = new List<Package>();
        public virtual ICollection<Package> ReceivedPackages { get; set; } = new List<Package>();
        public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
    }
}
