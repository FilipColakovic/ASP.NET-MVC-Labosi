namespace Vjezba.Model.Models
{
    public interface IObjectDetailsPageViewModel
    {
        string ObjectType { get; }
        int ObjectId { get; }
    }

    public sealed class PackageDetailsViewModel : IObjectDetailsPageViewModel
    {
        public string ObjectType => "package";
        public int ObjectId => Package.Id;
        public Package Package { get; init; } = null!;
        public int StatusLogCount { get; init; }
        public int WarehouseCount { get; init; }
        public int DeliveryCount { get; init; }
    }

    public sealed class CourierDetailsViewModel : IObjectDetailsPageViewModel
    {
        public string ObjectType => "courier";
        public int ObjectId => Courier.Id;
        public Courier Courier { get; init; } = null!;
        public int PackageCount { get; init; }
        public int DeliveryCount { get; init; }
    }

    public sealed class UserDetailsViewModel : IObjectDetailsPageViewModel
    {
        public string ObjectType => "user";
        public int ObjectId => User.Id;
        public User User { get; init; } = null!;
        public int SentPackageCount { get; init; }
        public int ReceivedPackageCount { get; init; }
    }

    public sealed class WarehouseDetailsViewModel : IObjectDetailsPageViewModel
    {
        public string ObjectType => "warehouse";
        public int ObjectId => Warehouse.Id;
        public Warehouse Warehouse { get; init; } = null!;
        public int StoredPackageCount { get; init; }
    }

    public sealed class DeliveryDetailsViewModel : IObjectDetailsPageViewModel
    {
        public string ObjectType => "delivery";
        public int ObjectId => Delivery.Id;
        public Delivery Delivery { get; init; } = null!;
        public int PackageCount { get; init; }
    }

    public sealed class AddressDetailsViewModel : IObjectDetailsPageViewModel
    {
        public string ObjectType => "address";
        public int ObjectId => Address.Id;
        public Address Address { get; init; } = null!;
        public int SentPackageCount { get; init; }
        public int ReceivedPackageCount { get; init; }
        public int WarehouseCount { get; init; }
    }

    public sealed class StatusLogDetailsViewModel : IObjectDetailsPageViewModel
    {
        public string ObjectType => "statuslog";
        public int ObjectId => StatusLog.Id;
        public StatusLog StatusLog { get; init; } = null!;
    }
}
