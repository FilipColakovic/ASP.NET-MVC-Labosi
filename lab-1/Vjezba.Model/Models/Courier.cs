namespace Vjezba.Model.Models
{
    public class Courier
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string VehicleType { get; set; }
        public string LicensePlate { get; set; }
        public bool IsAvailable { get; set; }
    }
}
