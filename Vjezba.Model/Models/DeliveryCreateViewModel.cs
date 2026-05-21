using System.ComponentModel.DataAnnotations;

namespace Vjezba.Model.Models
{
    public class DeliveryCreateViewModel
    {
        [Required]
        public DateTime DepartureDate { get; set; }

        [Required]
        public DateTime ArrivalDate { get; set; }

        [Required]
        public string CurrentLocation { get; set; } = string.Empty;

        public bool IsDelayed { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Courier is required.")]
        public int CourierId { get; set; }
    }
}
