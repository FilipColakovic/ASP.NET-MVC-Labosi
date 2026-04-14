using Vjezba.Model.Enums;
using Vjezba.Model.Models;

namespace Vjezba.Model.Data
{
    public sealed class SeedDataContext
    {
        public List<Courier> Couriers { get; init; } = new();
        public List<User> Users { get; init; } = new();
        public List<Address> Addresses { get; init; } = new();
        public List<Package> Packages { get; init; } = new();
        public List<Warehouse> Warehouses { get; init; } = new();
        public List<Delivery> Deliveries { get; init; } = new();
    }

    public static class SeedDataFactory
    {
        public static SeedDataContext Create()
        {
            var couriers = new List<Courier>
            {
                new() { Id = 1, FirstName = "Marko", LastName = "Horvat", Email = "marko@delivery.hr", PhoneNumber = "+385911111111", VehicleType = "Van", LicensePlate = "ZG-111-AA", IsAvailable = true },
                new() { Id = 2, FirstName = "Ana", LastName = "Kovac", Email = "ana@delivery.hr", PhoneNumber = "+385922222222", VehicleType = "Bike", LicensePlate = "ZG-222-BB", IsAvailable = true },
                new() { Id = 3, FirstName = "Ivan", LastName = "Babic", Email = "ivan@delivery.hr", PhoneNumber = "+385933333333", VehicleType = "Truck", LicensePlate = "ST-333-CC", IsAvailable = false }
            };

            var users = new List<User>
            {
                new() { Id = 1, FirstName = "Luka", LastName = "Matic", Email = "luka@example.com", PhoneNumber = "+385981111111" },
                new() { Id = 2, FirstName = "Petra", LastName = "Novak", Email = "petra@example.com", PhoneNumber = "+385982222222" },
                new() { Id = 3, FirstName = "Nikola", LastName = "Peric", Email = "nikola@example.com", PhoneNumber = "+385983333333" },
                new() { Id = 4, FirstName = "Tea", LastName = "Juric", Email = "tea@example.com", PhoneNumber = "+385984444444" },
                new() { Id = 5, FirstName = "Mia", LastName = "Klaric", Email = "mia@example.com", PhoneNumber = "+385985555555" },
                new() { Id = 6, FirstName = "Filip", LastName = "Radic", Email = "filip@example.com", PhoneNumber = "+385986666666" }
            };

            var addresses = new List<Address>
            {
                new() { Id = 1, Street = "Ilica 10", City = "Zagreb", PostalCode = "10000", Country = "Croatia" },
                new() { Id = 2, Street = "Vukovarska 55", City = "Zagreb", PostalCode = "10000", Country = "Croatia" },
                new() { Id = 3, Street = "Marmontova 12", City = "Split", PostalCode = "21000", Country = "Croatia" },
                new() { Id = 4, Street = "Korzo 20", City = "Rijeka", PostalCode = "51000", Country = "Croatia" },
                new() { Id = 5, Street = "Strossmayerova 8", City = "Osijek", PostalCode = "31000", Country = "Croatia" },
                new() { Id = 6, Street = "Trg Bana Jelacica 1", City = "Varazdin", PostalCode = "42000", Country = "Croatia" }
            };

            var packages = new List<Package>
            {
                CreatePackage(1, "HR000001", couriers[0], users[0], users[1], addresses[0], addresses[2], PackageStatus.InTransit, DeliveryPriority.Normal, "Documents"),
                CreatePackage(2, "HR000002", couriers[1], users[2], users[3], addresses[3], addresses[1], PackageStatus.OutForDelivery, DeliveryPriority.High, "Electronics"),
                CreatePackage(3, "HR000003", couriers[2], users[4], users[5], addresses[4], addresses[5], PackageStatus.Delivered, DeliveryPriority.Urgent, "Medicine"),
                CreatePackage(4, "HR000004", couriers[0], users[1], users[5], addresses[2], addresses[0], PackageStatus.Created, DeliveryPriority.Low, "Home appliances"),
                CreatePackage(5, "HR000005", couriers[2], users[3], users[0], addresses[5], addresses[4], PackageStatus.PendingPickup, DeliveryPriority.Normal, "Books"),
                CreatePackage(6, "HR000006", couriers[1], users[5], users[2], addresses[1], addresses[3], PackageStatus.DeliveryFailed, DeliveryPriority.VeryLow, "Clothes"),
                CreatePackage(7, "HR000007", couriers[0], users[0], users[4], addresses[0], addresses[4], PackageStatus.PickedUp, DeliveryPriority.High, "Laptop"),
                CreatePackage(8, "HR000008", couriers[1], users[1], users[2], addresses[1], addresses[3], PackageStatus.InTransit, DeliveryPriority.Normal, "Shoes"),
                CreatePackage(9, "HR000009", couriers[2], users[2], users[3], addresses[3], addresses[5], PackageStatus.PendingPickup, DeliveryPriority.Low, "Office supplies")
            };

            var warehouses = new List<Warehouse>
            {
                new() { Id = 1, Name = "Central Zagreb", Address = addresses[0], Capacity = 500, StoredPackages = new List<Package> { packages[0], packages[1], packages[2] } },
                new() { Id = 2, Name = "Adriatic Split", Address = addresses[2], Capacity = 350, StoredPackages = new List<Package> { packages[3], packages[4], packages[5] } },
                new() { Id = 3, Name = "East Osijek", Address = addresses[4], Capacity = 300, StoredPackages = new List<Package> { packages[6], packages[7], packages[8] } }
            };

            var deliveries = new List<Delivery>
            {
                new() { Id = 1, DepartureDate = DateTime.UtcNow.AddHours(-8), ArrivalDate = DateTime.UtcNow.AddHours(2), CurrentLocation = "Karlovac", IsDelayed = false, Courier = couriers[0], Packages = new List<Package> { packages[0], packages[3], packages[6] } },
                new() { Id = 2, DepartureDate = DateTime.UtcNow.AddHours(-6), ArrivalDate = DateTime.UtcNow.AddHours(1), CurrentLocation = "Zagreb", IsDelayed = false, Courier = couriers[1], Packages = new List<Package> { packages[1], packages[5], packages[7] } },
                new() { Id = 3, DepartureDate = DateTime.UtcNow.AddHours(-12), ArrivalDate = DateTime.UtcNow.AddHours(-2), CurrentLocation = "Varazdin", IsDelayed = true, Courier = couriers[2], Packages = new List<Package> { packages[2], packages[4], packages[8] } }
            };

            return new SeedDataContext
            {
                Couriers = couriers,
                Users = users,
                Addresses = addresses,
                Packages = packages,
                Warehouses = warehouses,
                Deliveries = deliveries
            };
        }

        private static Package CreatePackage(
            int id,
            string trackingNumber,
            Courier courier,
            User sender,
            User recipient,
            Address senderAddress,
            Address recipientAddress,
            PackageStatus status,
            DeliveryPriority priority,
            string description)
        {
            var package = new Package
            {
                Id = id,
                TrackingNumber = trackingNumber,
                WeightKg = 1.5m + id,
                DeliveryPriority = priority,
                Courier = courier,
                SenderUser = sender,
                RecipientUser = recipient,
                SenderAddress = senderAddress,
                RecipientAddress = recipientAddress,
                Status = status,
                Description = description,
                CreatedAt = DateTime.UtcNow.AddHours(-id * 3)
            };

            package.StatusHistory = new List<StatusLog>
            {
                new()
                {
                    Id = id * 10 + 1,
                    TimeChanged = package.CreatedAt,
                    Location = senderAddress.City,
                    Description = "Package created",
                    PreviousStatus = PackageStatus.Created,
                    NewStatus = PackageStatus.PendingPickup,
                    Package = package
                },
                new()
                {
                    Id = id * 10 + 2,
                    TimeChanged = DateTime.UtcNow.AddHours(-id),
                    Location = recipientAddress.City,
                    Description = "Package status updated",
                    PreviousStatus = PackageStatus.PendingPickup,
                    NewStatus = status,
                    Package = package
                }
            };

            return package;
        }
    }
}
