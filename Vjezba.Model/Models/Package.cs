using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Vjezba.Model.Enums;

namespace Vjezba.Model.Models
{
    public class Package
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string TrackingNumber { get; set; } = string.Empty;
        public decimal WeightKg { get; set; }
        public DeliveryPriority DeliveryPriority { get; set; }

        [ForeignKey(nameof(Courier))]
        public int CourierId { get; set; }

        [ForeignKey(nameof(SenderUser))]
        public int SenderUserId { get; set; }

        [ForeignKey(nameof(RecipientUser))]
        public int RecipientUserId { get; set; }

        [ForeignKey(nameof(SenderAddress))]
        public int SenderAddressId { get; set; }

        [ForeignKey(nameof(RecipientAddress))]
        public int RecipientAddressId { get; set; }

        public virtual Courier Courier { get; set; } = null!;
        public virtual User SenderUser { get; set; } = null!;
        public virtual User RecipientUser { get; set; } = null!;
        public virtual Address SenderAddress { get; set; } = null!;
        public virtual Address RecipientAddress { get; set; } = null!;
        public PackageStatus Status { get; set; }
        public virtual ICollection<StatusLog> StatusHistory { get; set; } = new List<StatusLog>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
        public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    }
}
