namespace Vjezba.Model.Models
{
    public class ObjectOverviewViewModel
    {
        public IReadOnlyList<Address> Addresses { get; init; } = Array.Empty<Address>();
        public IReadOnlyList<Courier> Couriers { get; init; } = Array.Empty<Courier>();
        public IReadOnlyList<User> Users { get; init; } = Array.Empty<User>();
        public IReadOnlyList<Package> Packages { get; init; } = Array.Empty<Package>();
        public IReadOnlyList<Warehouse> Warehouses { get; init; } = Array.Empty<Warehouse>();
        public IReadOnlyList<Delivery> Deliveries { get; init; } = Array.Empty<Delivery>();
    }
}
