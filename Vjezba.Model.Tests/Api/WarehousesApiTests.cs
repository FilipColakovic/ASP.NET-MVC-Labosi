using System.Net;
using System.Net.Http.Json;
using Vjezba.Model.Dtos.Api;
using Vjezba.Model.Tests.Infrastructure;

namespace Vjezba.Model.Tests.Api
{
    public sealed class WarehousesApiTests : ApiTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public WarehousesApiTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithSeededWarehouses()
        {
            var response = await Client.GetAsync("/api/warehouses");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var warehouses = await response.Content.ReadFromJsonAsync<List<WarehouseResponseDto>>();
            Assert.NotNull(warehouses);
            Assert.NotEmpty(warehouses);
        }

        [Fact]
        public async Task GetAll_WithSearch_ReturnsMatchingWarehouses()
        {
            var response = await Client.GetAsync("/api/warehouses?q=Central");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var warehouses = await response.Content.ReadFromJsonAsync<List<WarehouseResponseDto>>();
            Assert.NotNull(warehouses);
            Assert.Contains(warehouses, x => x.Name == "Central Zagreb");
        }

        [Fact]
        public async Task GetById_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.GetAsync("/api/warehouses/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetById_WhenAuthenticatedAndExists_ReturnsWarehouse()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/warehouses/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var warehouse = await response.Content.ReadFromJsonAsync<WarehouseResponseDto>();
            Assert.NotNull(warehouse);
            Assert.Equal(1, warehouse.Id);
            Assert.Equal("Central Zagreb", warehouse.Name);
        }

        [Fact]
        public async Task GetById_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/warehouses/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenManagerAndPayloadValid_ReturnsCreated()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildWarehouseRequest("Create");

            var response = await client.PostAsJsonAsync("/api/warehouses", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<WarehouseResponseDto>();
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal(request.Name, created.Name);
        }

        [Fact]
        public async Task Create_WhenPayloadInvalid_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = new
            {
                Name = (string?)null,
                AddressId = 0,
                Capacity = -1
            };

            var response = await client.PostAsJsonAsync("/api/warehouses", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.PostAsJsonAsync("/api/warehouses", BuildWarehouseRequest("Anonymous"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenManagerAndPayloadValid_ReturnsUpdatedWarehouse()
        {
            var client = CreateAuthenticatedClient("Manager");
            var created = await CreateWarehouseAsync(client, "UpdateOriginal");
            var request = BuildWarehouseRequest("UpdateChanged", created.Id);

            var response = await client.PutAsJsonAsync($"/api/warehouses/{created.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var updated = await response.Content.ReadFromJsonAsync<WarehouseResponseDto>();
            Assert.NotNull(updated);
            Assert.Equal(created.Id, updated.Id);
            Assert.Equal(request.Name, updated.Name);
        }

        [Fact]
        public async Task Update_WhenRouteAndBodyIdsMismatch_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildWarehouseRequest("Mismatch", id: 123);

            var response = await client.PutAsJsonAsync("/api/warehouses/456", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildWarehouseRequest("Missing");

            var response = await client.PutAsJsonAsync("/api/warehouses/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenManager_ReturnsForbidden()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var created = await CreateWarehouseAsync(managerClient, "ManagerDeleteForbidden");

            var response = await managerClient.DeleteAsync($"/api/warehouses/{created.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenAdminAndExists_ReturnsNoContent()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var adminClient = CreateAuthenticatedClient("Admin");
            var created = await CreateWarehouseAsync(managerClient, "AdminDelete");

            var response = await adminClient.DeleteAsync($"/api/warehouses/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getDeleted = await adminClient.GetAsync($"/api/warehouses/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Admin");

            var response = await client.DeleteAsync("/api/warehouses/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static WarehouseUpsertRequestDto BuildWarehouseRequest(string suffix, int id = 0)
        {
            return new WarehouseUpsertRequestDto
            {
                Id = id,
                Name = $"Warehouse {suffix} {Guid.NewGuid():N}",
                AddressId = 1,
                Capacity = 123
            };
        }

        private static async Task<WarehouseResponseDto> CreateWarehouseAsync(HttpClient client, string suffix)
        {
            var response = await client.PostAsJsonAsync("/api/warehouses", BuildWarehouseRequest(suffix));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<WarehouseResponseDto>();
            Assert.NotNull(created);
            return created;
        }
    }
}
