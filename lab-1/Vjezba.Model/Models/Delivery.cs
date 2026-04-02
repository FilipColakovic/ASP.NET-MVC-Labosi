namespace Vjezba.Model.Models
{
    public class Delivery
    {
        public int Id { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }
        public string CurrentLocation { get; set; } = string.Empty;
        public bool IsDelayed { get; set; }
        public Courier Courier { get; set; } = null!;
        public List<Package> Packages { get; set; } = new List<Package>();
    }
}
