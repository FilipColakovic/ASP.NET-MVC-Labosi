using System.Net;
using System.Net.Http.Json;
using Vjezba.Model.Dtos.Api;
using Vjezba.Model.Enums;
using Vjezba.Model.Tests.Infrastructure;

namespace Vjezba.Model.Tests.Api
{
    public sealed class StatusLogsApiTests : ApiTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public StatusLogsApiTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithSeededStatusLogs()
        {
            var response = await Client.GetAsync("/api/status-logs");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var statusLogs = await response.Content.ReadFromJsonAsync<List<StatusLogResponseDto>>();
            Assert.NotNull(statusLogs);
            Assert.NotEmpty(statusLogs);
        }

        [Fact]
        public async Task GetAll_WithSearch_ReturnsMatchingStatusLogs()
        {
            var response = await Client.GetAsync("/api/status-logs?q=HR000001");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var statusLogs = await response.Content.ReadFromJsonAsync<List<StatusLogResponseDto>>();
            Assert.NotNull(statusLogs);
            Assert.Contains(statusLogs, x => x.Package?.TrackingNumber == "HR000001");
        }

        [Fact]
        public async Task GetById_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.GetAsync("/api/status-logs/11");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetById_WhenAuthenticatedAndExists_ReturnsStatusLog()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/status-logs/11");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var statusLog = await response.Content.ReadFromJsonAsync<StatusLogResponseDto>();
            Assert.NotNull(statusLog);
            Assert.Equal(11, statusLog.Id);
            Assert.Equal("Package created", statusLog.Description);
        }

        [Fact]
        public async Task GetById_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/status-logs/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenManagerAndPayloadValid_ReturnsCreated()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildStatusLogRequest("Create");

            var response = await client.PostAsJsonAsync("/api/status-logs", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<StatusLogResponseDto>();
            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal(request.Description, created.Description);
            Assert.NotNull(created.Package);
        }

        [Fact]
        public async Task Create_WhenPayloadInvalid_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = new
            {
                TimeChanged = DateTime.UtcNow,
                Location = (string?)null,
                Description = (string?)null,
                PreviousStatus = PackageStatus.Created,
                NewStatus = PackageStatus.InTransit,
                PackageId = 0
            };

            var response = await client.PostAsJsonAsync("/api/status-logs", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.PostAsJsonAsync("/api/status-logs", BuildStatusLogRequest("Anonymous"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenManagerAndPayloadValid_ReturnsUpdatedStatusLog()
        {
            var client = CreateAuthenticatedClient("Manager");
            var created = await CreateStatusLogAsync(client, "UpdateOriginal");
            var request = BuildStatusLogRequest("UpdateChanged", created.Id);

            var response = await client.PutAsJsonAsync($"/api/status-logs/{created.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var updated = await response.Content.ReadFromJsonAsync<StatusLogResponseDto>();
            Assert.NotNull(updated);
            Assert.Equal(created.Id, updated.Id);
            Assert.Equal(request.Description, updated.Description);
        }

        [Fact]
        public async Task Update_WhenRouteAndBodyIdsMismatch_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildStatusLogRequest("Mismatch", id: 123);

            var response = await client.PutAsJsonAsync("/api/status-logs/456", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildStatusLogRequest("Missing");

            var response = await client.PutAsJsonAsync("/api/status-logs/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenManager_ReturnsForbidden()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var created = await CreateStatusLogAsync(managerClient, "ManagerDeleteForbidden");

            var response = await managerClient.DeleteAsync($"/api/status-logs/{created.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenAdminAndExists_ReturnsNoContent()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var adminClient = CreateAuthenticatedClient("Admin");
            var created = await CreateStatusLogAsync(managerClient, "AdminDelete");

            var response = await adminClient.DeleteAsync($"/api/status-logs/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getDeleted = await adminClient.GetAsync($"/api/status-logs/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Admin");

            var response = await client.DeleteAsync("/api/status-logs/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static StatusLogUpsertRequestDto BuildStatusLogRequest(string suffix, int id = 0)
        {
            return new StatusLogUpsertRequestDto
            {
                Id = id,
                TimeChanged = DateTime.UtcNow,
                Location = $"Integration Hub {suffix}",
                Description = $"Integration status log {suffix} {Guid.NewGuid():N}",
                PreviousStatus = PackageStatus.Created,
                NewStatus = PackageStatus.InTransit,
                PackageId = 1
            };
        }

        private static async Task<StatusLogResponseDto> CreateStatusLogAsync(HttpClient client, string suffix)
        {
            var response = await client.PostAsJsonAsync("/api/status-logs", BuildStatusLogRequest(suffix));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<StatusLogResponseDto>();
            Assert.NotNull(created);
            return created;
        }
    }
}
