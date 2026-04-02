namespace Vjezba.Model.Models
{
    public class Warehouse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; }
        public int Capacity { get; set; }
        public List<Package> StoredPackages { get; set; } = new List<Package>();
    }
}
