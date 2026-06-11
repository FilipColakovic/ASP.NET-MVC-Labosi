using System.Net;
using System.Net.Http.Json;
using Vjezba.Model.Dtos.Api;
using Vjezba.Model.Tests.Infrastructure;

namespace Vjezba.Model.Tests.Api
{
    public sealed class AddressesApiTests : ApiTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public AddressesApiTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithSeededAddresses()
        {
            var response = await Client.GetAsync("/api/addresses");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var addresses = await response.Content.ReadFromJsonAsync<List<AddressResponseDto>>();
            Assert.NotNull(addresses);
            Assert.NotEmpty(addresses);
        }

        [Fact]
        public async Task GetAll_WithSearch_ReturnsMatchingAddresses()
        {
            var response = await Client.GetAsync("/api/addresses?q=Ilica");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var addresses = await response.Content.ReadFromJsonAsync<List<AddressResponseDto>>();
            Assert.NotNull(addresses);
            Assert.Contains(addresses, x => x.Street == "Ilica 10");
            Assert.All(addresses, x =>
                Assert.True(
                    x.Street.Contains("Ilica", StringComparison.OrdinalIgnoreCase)
                    || x.City.Contains("Ilica", StringComparison.OrdinalIgnoreCase)
                    || x.PostalCode.Contains("Ilica", StringComparison.OrdinalIgnoreCase)
                    || x.Country.Contains("Ilica", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public async Task GetById_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.GetAsync("/api/addresses/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetById_WhenAuthenticatedAndExists_ReturnsAddress()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/addresses/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var address = await response.Content.ReadFromJsonAsync<AddressResponseDto>();
            Assert.NotNull(address);
            Assert.Equal(1, address.Id);
            Assert.Equal("Ilica 10", address.Street);
        }

        [Fact]
        public async Task GetById_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/addresses/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenManagerAndPayloadValid_ReturnsCreated()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildAddressRequest("Integration Create Street");

            var response = await client.PostAsJsonAsync("/api/addresses", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<AddressResponseDto>();
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal(request.Street, created.Street);
        }

        [Fact]
        public async Task Create_WhenPayloadInvalid_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = new
            {
                Street = (string?)null,
                City = "Test City",
                PostalCode = "10000",
                Country = "Croatia"
            };

            var response = await client.PostAsJsonAsync("/api/addresses", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.PostAsJsonAsync("/api/addresses", BuildAddressRequest("Anonymous Create Street"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenManagerAndPayloadValid_ReturnsUpdatedAddress()
        {
            var client = CreateAuthenticatedClient("Manager");
            var created = await CreateAddressAsync(client, "Integration Update Original");
            var request = BuildAddressRequest("Integration Update Changed", created.Id);

            var response = await client.PutAsJsonAsync($"/api/addresses/{created.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var updated = await response.Content.ReadFromJsonAsync<AddressResponseDto>();
            Assert.NotNull(updated);
            Assert.Equal(created.Id, updated.Id);
            Assert.Equal("Integration Update Changed", updated.Street);
        }

        [Fact]
        public async Task Update_WhenRouteAndBodyIdsMismatch_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildAddressRequest("Mismatch Street", id: 123);

            var response = await client.PutAsJsonAsync("/api/addresses/456", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildAddressRequest("Missing Street");

            var response = await client.PutAsJsonAsync("/api/addresses/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenManager_ReturnsForbidden()
        {
            var client = CreateAuthenticatedClient("Manager");
            var created = await CreateAddressAsync(client, "Manager Delete Forbidden Street");

            var response = await client.DeleteAsync($"/api/addresses/{created.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenAdminAndExists_ReturnsNoContent()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var adminClient = CreateAuthenticatedClient("Admin");
            var created = await CreateAddressAsync(managerClient, "Admin Delete Street");

            var response = await adminClient.DeleteAsync($"/api/addresses/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getDeleted = await adminClient.GetAsync($"/api/addresses/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Admin");

            var response = await client.DeleteAsync("/api/addresses/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static AddressUpsertRequestDto BuildAddressRequest(string street, int id = 0)
        {
            return new AddressUpsertRequestDto
            {
                Id = id,
                Street = street,
                City = "Integration City",
                PostalCode = "10000",
                Country = "Croatia"
            };
        }

        private static async Task<AddressResponseDto> CreateAddressAsync(HttpClient client, string street)
        {
            var response = await client.PostAsJsonAsync("/api/addresses", BuildAddressRequest(street));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<AddressResponseDto>();
            Assert.NotNull(created);
            return created;
        }
    }
}
