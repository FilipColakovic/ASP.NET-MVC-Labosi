using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vjezba.Model.Models
{
    public class Delivery
    {
        [Key]
        public int Id { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }

        [Required]
        public string CurrentLocation { get; set; } = string.Empty;
        public bool IsDelayed { get; set; }

        [ForeignKey(nameof(Courier))]
        public int CourierId { get; set; }

        public virtual Courier Courier { get; set; } = null!;
        public virtual ICollection<Package> Packages { get; set; } = new List<Package>();
    }
}
