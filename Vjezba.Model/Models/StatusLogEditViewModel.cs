using System.ComponentModel.DataAnnotations;
using Vjezba.Model.Enums;

namespace Vjezba.Model.Models
{
    public class StatusLogEditViewModel
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public DateTime TimeChanged { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public PackageStatus PreviousStatus { get; set; }
        public PackageStatus NewStatus { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Package is required.")]
        public int PackageId { get; set; }
    }
}
