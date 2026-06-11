using System.Net;
using System.Net.Http.Json;
using Vjezba.Model.Dtos.Api;
using Vjezba.Model.Enums;
using Vjezba.Model.Tests.Infrastructure;

namespace Vjezba.Model.Tests.Api
{
    public sealed class PackagesApiTests : ApiTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public PackagesApiTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithSeededPackages()
        {
            var response = await Client.GetAsync("/api/packages");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var packages = await response.Content.ReadFromJsonAsync<List<PackageResponseDto>>();
            Assert.NotNull(packages);
            Assert.NotEmpty(packages);
        }

        [Fact]
        public async Task GetAll_WithSearch_ReturnsMatchingPackages()
        {
            var response = await Client.GetAsync("/api/packages?q=HR000001");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var packages = await response.Content.ReadFromJsonAsync<List<PackageResponseDto>>();
            Assert.NotNull(packages);
            Assert.Contains(packages, x => x.TrackingNumber == "HR000001");
        }

        [Fact]
        public async Task GetById_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.GetAsync("/api/packages/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetById_WhenAuthenticatedAndExists_ReturnsPackage()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/packages/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var package = await response.Content.ReadFromJsonAsync<PackageResponseDto>();
            Assert.NotNull(package);
            Assert.Equal(1, package.Id);
            Assert.Equal("HR000001", package.TrackingNumber);
        }

        [Fact]
        public async Task GetById_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/packages/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenManagerAndPayloadValid_ReturnsCreated()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildPackageRequest("Create");

            var response = await client.PostAsJsonAsync("/api/packages", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<PackageResponseDto>();
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal(request.TrackingNumber, created.TrackingNumber);
            Assert.NotNull(created.Courier);
        }

        [Fact]
        public async Task Create_WhenPayloadInvalid_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = new
            {
                TrackingNumber = (string?)null,
                WeightKg = 0,
                DeliveryPriority = DeliveryPriority.Normal,
                CourierId = 0,
                SenderUserId = 0,
                RecipientUserId = 0,
                SenderAddressId = 0,
                RecipientAddressId = 0,
                Status = PackageStatus.Created,
                Description = (string?)null
            };

            var response = await client.PostAsJsonAsync("/api/packages", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.PostAsJsonAsync("/api/packages", BuildPackageRequest("Anonymous"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenManagerAndPayloadValid_ReturnsUpdatedPackage()
        {
            var client = CreateAuthenticatedClient("Manager");
            var created = await CreatePackageAsync(client, "UpdateOriginal");
            var request = BuildPackageRequest("UpdateChanged", created.Id);

            var response = await client.PutAsJsonAsync($"/api/packages/{created.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var updated = await response.Content.ReadFromJsonAsync<PackageResponseDto>();
            Assert.NotNull(updated);
            Assert.Equal(created.Id, updated.Id);
            Assert.Equal(request.TrackingNumber, updated.TrackingNumber);
        }

        [Fact]
        public async Task Update_WhenRouteAndBodyIdsMismatch_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildPackageRequest("Mismatch", id: 123);

            var response = await client.PutAsJsonAsync("/api/packages/456", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildPackageRequest("Missing");

            var response = await client.PutAsJsonAsync("/api/packages/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenManager_ReturnsForbidden()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var created = await CreatePackageAsync(managerClient, "ManagerDeleteForbidden");

            var response = await managerClient.DeleteAsync($"/api/packages/{created.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenAdminAndExists_ReturnsNoContent()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var adminClient = CreateAuthenticatedClient("Admin");
            var created = await CreatePackageAsync(managerClient, "AdminDelete");

            var response = await adminClient.DeleteAsync($"/api/packages/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getDeleted = await adminClient.GetAsync($"/api/packages/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Admin");

            var response = await client.DeleteAsync("/api/packages/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static PackageUpsertRequestDto BuildPackageRequest(string suffix, int id = 0)
        {
            return new PackageUpsertRequestDto
            {
                Id = id,
                TrackingNumber = $"IT-{suffix}-{Guid.NewGuid():N}".ToUpperInvariant(),
                WeightKg = 2.5m,
                DeliveryPriority = DeliveryPriority.Normal,
                CourierId = 1,
                SenderUserId = 1,
                RecipientUserId = 2,
                SenderAddressId = 1,
                RecipientAddressId = 2,
                Status = PackageStatus.Created,
                Description = $"Integration package {suffix}"
            };
        }

        private static async Task<PackageResponseDto> CreatePackageAsync(HttpClient client, string suffix)
        {
            var response = await client.PostAsJsonAsync("/api/packages", BuildPackageRequest(suffix));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<PackageResponseDto>();
            Assert.NotNull(created);
            return created;
        }
    }
}
