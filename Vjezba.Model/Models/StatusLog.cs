using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Vjezba.Model.Enums;

namespace Vjezba.Model.Models
{
    public class StatusLog
    {
        [Key]
        public int Id { get; set; }
        public DateTime TimeChanged { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;
        public PackageStatus PreviousStatus { get; set; }
        public PackageStatus NewStatus { get; set; }

        [ForeignKey(nameof(Package))]
        public int PackageId { get; set; }

        public virtual Package Package { get; set; } = null!;
    }
}
