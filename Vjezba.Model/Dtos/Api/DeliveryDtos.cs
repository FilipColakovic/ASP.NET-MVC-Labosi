using System.ComponentModel.DataAnnotations;

namespace Vjezba.Model.Dtos.Api
{
    public class DeliveryUpsertRequestDto
    {
        public int Id { get; set; }

        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }

        [Required]
        public string CurrentLocation { get; set; } = string.Empty;

        public bool IsDelayed { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Courier is required.")]
        public int CourierId { get; set; }
    }

    public class DeliveryResponseDto
    {
        public int Id { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }
        public string CurrentLocation { get; set; } = string.Empty;
        public bool IsDelayed { get; set; }
        public CourierSummaryDto? Courier { get; set; }
        public List<PackageSummaryDto> Packages { get; set; } = new();
    }
}
