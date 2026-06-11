using Microsoft.AspNetCore.Mvc.Testing;

namespace Vjezba.Model.Tests.Infrastructure
{
    public abstract class ApiTestBase
    {
        protected ApiTestBase(CustomWebApplicationFactory factory)
        {
            Factory = factory;
            Client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        protected CustomWebApplicationFactory Factory { get; }

        protected HttpClient Client { get; }

        protected HttpClient CreateAuthenticatedClient(string role)
        {
            var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeaderName, role);
            return client;
        }

        protected static string UniqueValue(string prefix)
        {
            return $"{prefix}-{Guid.NewGuid():N}";
        }
    }
}
