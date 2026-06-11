using System.Net;
using System.Net.Http.Json;
using Vjezba.Model.Dtos.Api;
using Vjezba.Model.Tests.Infrastructure;

namespace Vjezba.Model.Tests.Api
{
    public sealed class DeliveriesApiTests : ApiTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public DeliveriesApiTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithSeededDeliveries()
        {
            var response = await Client.GetAsync("/api/deliveries");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var deliveries = await response.Content.ReadFromJsonAsync<List<DeliveryResponseDto>>();
            Assert.NotNull(deliveries);
            Assert.NotEmpty(deliveries);
        }

        [Fact]
        public async Task GetAll_WithSearch_ReturnsMatchingDeliveries()
        {
            var response = await Client.GetAsync("/api/deliveries?q=Karlovac");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var deliveries = await response.Content.ReadFromJsonAsync<List<DeliveryResponseDto>>();
            Assert.NotNull(deliveries);
            Assert.Contains(deliveries, x => x.CurrentLocation == "Karlovac");
        }

        [Fact]
        public async Task GetById_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.GetAsync("/api/deliveries/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetById_WhenAuthenticatedAndExists_ReturnsDelivery()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/deliveries/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var delivery = await response.Content.ReadFromJsonAsync<DeliveryResponseDto>();
            Assert.NotNull(delivery);
            Assert.Equal(1, delivery.Id);
            Assert.Equal("Karlovac", delivery.CurrentLocation);
        }

        [Fact]
        public async Task GetById_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/deliveries/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenManagerAndPayloadValid_ReturnsCreated()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildDeliveryRequest("Create");

            var response = await client.PostAsJsonAsync("/api/deliveries", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<DeliveryResponseDto>();
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal(request.CurrentLocation, created.CurrentLocation);
            Assert.NotNull(created.Courier);
        }

        [Fact]
        public async Task Create_WhenPayloadInvalid_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = new
            {
                DepartureDate = DateTime.UtcNow,
                ArrivalDate = DateTime.UtcNow.AddHours(1),
                CurrentLocation = (string?)null,
                IsDelayed = false,
                CourierId = 0
            };

            var response = await client.PostAsJsonAsync("/api/deliveries", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.PostAsJsonAsync("/api/deliveries", BuildDeliveryRequest("Anonymous"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenManagerAndPayloadValid_ReturnsUpdatedDelivery()
        {
            var client = CreateAuthenticatedClient("Manager");
            var created = await CreateDeliveryAsync(client, "UpdateOriginal");
            var request = BuildDeliveryRequest("UpdateChanged", created.Id);

            var response = await client.PutAsJsonAsync($"/api/deliveries/{created.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var updated = await response.Content.ReadFromJsonAsync<DeliveryResponseDto>();
            Assert.NotNull(updated);
            Assert.Equal(created.Id, updated.Id);
            Assert.Equal(request.CurrentLocation, updated.CurrentLocation);
        }

        [Fact]
        public async Task Update_WhenRouteAndBodyIdsMismatch_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildDeliveryRequest("Mismatch", id: 123);

            var response = await client.PutAsJsonAsync("/api/deliveries/456", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildDeliveryRequest("Missing");

            var response = await client.PutAsJsonAsync("/api/deliveries/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenManager_ReturnsForbidden()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var created = await CreateDeliveryAsync(managerClient, "ManagerDeleteForbidden");

            var response = await managerClient.DeleteAsync($"/api/deliveries/{created.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenAdminAndExists_ReturnsNoContent()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var adminClient = CreateAuthenticatedClient("Admin");
            var created = await CreateDeliveryAsync(managerClient, "AdminDelete");

            var response = await adminClient.DeleteAsync($"/api/deliveries/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getDeleted = await adminClient.GetAsync($"/api/deliveries/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Admin");

            var response = await client.DeleteAsync("/api/deliveries/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static DeliveryUpsertRequestDto BuildDeliveryRequest(string suffix, int id = 0)
        {
            var departure = DateTime.UtcNow.AddMinutes(-15);
            return new DeliveryUpsertRequestDto
            {
                Id = id,
                DepartureDate = departure,
                ArrivalDate = departure.AddHours(2),
                CurrentLocation = $"Integration Location {suffix} {Guid.NewGuid():N}",
                IsDelayed = false,
                CourierId = 1
            };
        }

        private static async Task<DeliveryResponseDto> CreateDeliveryAsync(HttpClient client, string suffix)
        {
            var response = await client.PostAsJsonAsync("/api/deliveries", BuildDeliveryRequest(suffix));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<DeliveryResponseDto>();
            Assert.NotNull(created);
            return created;
        }
    }
}
