using System.ComponentModel.DataAnnotations;
using Vjezba.Model.Enums;

namespace Vjezba.Model.Dtos.Api
{
    public class StatusLogUpsertRequestDto
    {
        public int Id { get; set; }

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

    public class StatusLogResponseDto
    {
        public int Id { get; set; }
        public DateTime TimeChanged { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PackageStatus PreviousStatus { get; set; }
        public PackageStatus NewStatus { get; set; }
        public PackageSummaryDto? Package { get; set; }
    }
}
