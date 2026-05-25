namespace Vjezba.Model.Dtos.Api
{
    public class AddressSummaryDto
    {
        public int Id { get; set; }
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class CourierSummaryDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
    }

    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class PackageSummaryDto
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
    }
}
