using System.Net;
using System.Net.Http.Json;
using Vjezba.Model.Dtos.Api;
using Vjezba.Model.Tests.Infrastructure;

namespace Vjezba.Model.Tests.Api
{
    public sealed class UsersApiTests : ApiTestBase, IClassFixture<CustomWebApplicationFactory>
    {
        public UsersApiTests(CustomWebApplicationFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithSeededUsers()
        {
            var response = await Client.GetAsync("/api/users");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var users = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>();
            Assert.NotNull(users);
            Assert.NotEmpty(users);
        }

        [Fact]
        public async Task GetAll_WithSearch_ReturnsMatchingUsers()
        {
            var response = await Client.GetAsync("/api/users?q=Luka");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var users = await response.Content.ReadFromJsonAsync<List<UserResponseDto>>();
            Assert.NotNull(users);
            Assert.Contains(users, x => x.FirstName == "Luka");
        }

        [Fact]
        public async Task GetById_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.GetAsync("/api/users/1");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetById_WhenAuthenticatedAndExists_ReturnsUser()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/users/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var user = await response.Content.ReadFromJsonAsync<UserResponseDto>();
            Assert.NotNull(user);
            Assert.Equal(1, user.Id);
            Assert.Equal("Luka", user.FirstName);
        }

        [Fact]
        public async Task GetById_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");

            var response = await client.GetAsync("/api/users/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenManagerAndPayloadValid_ReturnsCreated()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildUserRequest("Create");

            var response = await client.PostAsJsonAsync("/api/users", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<UserResponseDto>();
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
                PhoneNumber = "123"
            };

            var response = await client.PostAsJsonAsync("/api/users", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_WhenAnonymous_ReturnsUnauthorized()
        {
            var response = await Client.PostAsJsonAsync("/api/users", BuildUserRequest("Anonymous"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenManagerAndPayloadValid_ReturnsUpdatedUser()
        {
            var client = CreateAuthenticatedClient("Manager");
            var created = await CreateUserAsync(client, "UpdateOriginal");
            var request = BuildUserRequest("UpdateChanged", created.Id);

            var response = await client.PutAsJsonAsync($"/api/users/{created.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var updated = await response.Content.ReadFromJsonAsync<UserResponseDto>();
            Assert.NotNull(updated);
            Assert.Equal(created.Id, updated.Id);
            Assert.Equal(request.Email, updated.Email);
        }

        [Fact]
        public async Task Update_WhenRouteAndBodyIdsMismatch_ReturnsBadRequest()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildUserRequest("Mismatch", id: 123);

            var response = await client.PutAsJsonAsync("/api/users/456", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Update_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Manager");
            var request = BuildUserRequest("Missing");

            var response = await client.PutAsJsonAsync("/api/users/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenManager_ReturnsForbidden()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var created = await CreateUserAsync(managerClient, "ManagerDeleteForbidden");

            var response = await managerClient.DeleteAsync($"/api/users/{created.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenAdminAndExists_ReturnsNoContent()
        {
            var managerClient = CreateAuthenticatedClient("Manager");
            var adminClient = CreateAuthenticatedClient("Admin");
            var created = await CreateUserAsync(managerClient, "AdminDelete");

            var response = await adminClient.DeleteAsync($"/api/users/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getDeleted = await adminClient.GetAsync($"/api/users/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);
        }

        [Fact]
        public async Task Delete_WhenMissing_ReturnsNotFound()
        {
            var client = CreateAuthenticatedClient("Admin");

            var response = await client.DeleteAsync("/api/users/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static UserUpsertRequestDto BuildUserRequest(string suffix, int id = 0)
        {
            var unique = Guid.NewGuid().ToString("N");
            return new UserUpsertRequestDto
            {
                Id = id,
                FirstName = $"Test{suffix}",
                LastName = "User",
                Email = $"user-{suffix}-{unique}@example.test".ToLowerInvariant(),
                PhoneNumber = "+385981234567"
            };
        }

        private static async Task<UserResponseDto> CreateUserAsync(HttpClient client, string suffix)
        {
            var response = await client.PostAsJsonAsync("/api/users", BuildUserRequest(suffix));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<UserResponseDto>();
            Assert.NotNull(created);
            return created;
        }
    }
}
