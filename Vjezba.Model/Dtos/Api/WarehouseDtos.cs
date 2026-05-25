using System.ComponentModel.DataAnnotations;

namespace Vjezba.Model.Dtos.Api
{
    public class WarehouseUpsertRequestDto
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Address is required.")]
        public int AddressId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Capacity must be zero or higher.")]
        public int Capacity { get; set; }
    }

    public class WarehouseResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public AddressSummaryDto? Address { get; set; }
        public int StoredPackageCount { get; set; }
    }
}
