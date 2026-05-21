using System.ComponentModel.DataAnnotations;
using Vjezba.Model.Enums;

namespace Vjezba.Model.Models
{
    public class PackageCreateViewModel
    {
        [Required]
        public string TrackingNumber { get; set; } = string.Empty;

        [Range(0.01, 100000, ErrorMessage = "Weight must be greater than zero.")]
        public decimal WeightKg { get; set; }

        public DeliveryPriority DeliveryPriority { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Courier is required.")]
        public int CourierId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Sender user is required.")]
        public int SenderUserId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Recipient user is required.")]
        public int RecipientUserId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Sender address is required.")]
        public int SenderAddressId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Recipient address is required.")]
        public int RecipientAddressId { get; set; }

        public PackageStatus Status { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public DateTime? DeliveredAt { get; set; }
    }
}
