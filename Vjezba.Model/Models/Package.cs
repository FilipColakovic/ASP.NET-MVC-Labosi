using Vjezba.Model.Enums;

namespace Vjezba.Model.Models
{
    public class Package
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public decimal WeightKg { get; set; }
        public DeliveryPriority DeliveryPriority { get; set; }
        public Courier Courier { get; set; } = null!;
        public User SenderUser { get; set; } = null!;
        public User RecipientUser { get; set; } = null!;
        public Address SenderAddress { get; set; } = null!;
        public Address RecipientAddress { get; set; } = null!;
        public PackageStatus Status { get; set; }
        public List<StatusLog> StatusHistory { get; set; } = new List<StatusLog>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
