using System.ComponentModel.DataAnnotations;

namespace Vjezba.Model.Models
{
    public class WarehouseEditViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Address is required.")]
        public int AddressId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Capacity must be zero or higher.")]
        public int Capacity { get; set; }
    }
}
