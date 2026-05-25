using Vjezba.Model.Models;

namespace Vjezba.Model.Dtos.Api
{
    public static class ApiDtoMapper
    {
        public static AddressSummaryDto ToSummaryDto(this Address model)
        {
            return new AddressSummaryDto
            {
                Id = model.Id,
                Street = model.Street,
                City = model.City,
                PostalCode = model.PostalCode,
                Country = model.Country
            };
        }

        public static CourierSummaryDto ToSummaryDto(this Courier model)
        {
            return new CourierSummaryDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                LicensePlate = model.LicensePlate
            };
        }

        public static UserSummaryDto ToSummaryDto(this User model)
        {
            return new UserSummaryDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email
            };
        }

        public static PackageSummaryDto ToSummaryDto(this Package model)
        {
            return new PackageSummaryDto
            {
                Id = model.Id,
                TrackingNumber = model.TrackingNumber
            };
        }
    }
}
