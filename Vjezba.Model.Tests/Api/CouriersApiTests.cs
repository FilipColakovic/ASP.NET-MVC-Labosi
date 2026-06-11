using System.Net;
using System.Net.Http.Json;
using Vjezba.Model.Dtos.Api;
using Vjezba.Model.Tests.Infrastructure;

namespace Vjezba.Model.Tests.Api
{
    public sealed class CouriersApiTests : ApiTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public CouriersApiTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithSeededCouriers()
        {
            var response = await Client.GetAsync("/api/couriers");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var couriers = await response.Content.ReadFromJsonAsync<List<CourierResponseDto>>();
            Assert.NotNull(couriers);
            Assert.NotEmpty(couriers);
        }

        [Fact]
        public async Task GetAll_WithSearch_ReturnsMatchingCouriers()
        {
            var response = await Client.GetAsync("/api/couriers?q=Marko");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var couriers = await response.Content.ReadFromJsonAsync<List<CourierResponseDto>>();
            Assert.NotNull(couriers);
            Assert.Contains(couriers, x => x.FirstName == "Marko");
        }

        [Fact]
        public async Task GetById_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.GetAsync("/api/couriers/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetById_WhenAuthenticatedAndExists_ReturnsCourier()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/couriers/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var courier = await response.Content.ReadFromJsonAsync<CourierResponseDto>();
            Assert.NotNull(courier);
            Assert.Equal(1, courier.Id);
            Assert.Equal("Marko", courier.FirstName);
        }

        [Fact]
        public async Task GetById_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/couriers/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenManagerAndPayloadValid_ReturnsCreated()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildCourierRequest("Create");

            var response = await client.PostAsJsonAsync("/api/couriers", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<CourierResponseDto>();
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal(request.Email, created.Email);
        }

        [Fact]
        public async Task Create_WhenPayloadInvalid_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = new
            {
                FirstName = (string?)null,
                LastName = "Invalid",
                Email = "not-an-email",
                PhoneNumber = "123",
                VehicleType = "Van",
                LicensePlate = UniqueValue("PLATE"),
                IsAvailable = true
            };

            var response = await client.PostAsJsonAsync("/api/couriers", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.PostAsJsonAsync("/api/couriers", BuildCourierRequest("Anonymous"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenManagerAndPayloadValid_ReturnsUpdatedCourier()
        {
            var client = CreateAuthenticatedClient("Manager");
            var created = await CreateCourierAsync(client, "UpdateOriginal");
            var request = BuildCourierRequest("UpdateChanged", created.Id);

            var response = await client.PutAsJsonAsync($"/api/couriers/{created.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var updated = await response.Content.ReadFromJsonAsync<CourierResponseDto>();
            Assert.NotNull(updated);
            Assert.Equal(created.Id, updated.Id);
            Assert.Equal(request.Email, updated.Email);
        }

        [Fact]
        public async Task Update_WhenRouteAndBodyIdsMismatch_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildCourierRequest("Mismatch", id: 123);

            var response = await client.PutAsJsonAsync("/api/couriers/456", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildCourierRequest("Missing");

            var response = await client.PutAsJsonAsync("/api/couriers/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenManager_ReturnsForbidden()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var created = await CreateCourierAsync(managerClient, "ManagerDeleteForbidden");

            var response = await managerClient.DeleteAsync($"/api/couriers/{created.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenAdminAndExists_ReturnsNoContent()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var adminClient = CreateAuthenticatedClient("Admin");
            var created = await CreateCourierAsync(managerClient, "AdminDelete");

            var response = await adminClient.DeleteAsync($"/api/couriers/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getDeleted = await adminClient.GetAsync($"/api/couriers/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Admin");

            var response = await client.DeleteAsync("/api/couriers/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static CourierUpsertRequestDto BuildCourierRequest(string suffix, int id = 0)
        {
            var unique = Guid.NewGuid().ToString("N");
            return new CourierUpsertRequestDto
            {
                Id = id,
                FirstName = $"Test{suffix}",
                LastName = "Courier",
                Email = $"courier-{suffix}-{unique}@example.test".ToLowerInvariant(),
                PhoneNumber = "+385990000000",
                VehicleType = "Van",
                LicensePlate = $"TEST-{unique[..8]}",
                IsAvailable = true
            };
        }

        private static async Task<CourierResponseDto> CreateCourierAsync(HttpClient client, string suffix)
        {
            var response = await client.PostAsJsonAsync("/api/couriers", BuildCourierRequest(suffix));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<CourierResponseDto>();
            Assert.NotNull(created);
            return created;
        }
    }
}
