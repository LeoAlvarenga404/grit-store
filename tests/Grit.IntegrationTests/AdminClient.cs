using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Grit.IntegrationTests;

internal static class AdminClient
{
    // The literal role Identity grants (Roles.Admin): if the policy's role drifted from it, these clients would get 403.
    public static HttpClient Create(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CustomerClient.Token(Guid.NewGuid(), "admin"));
        return client;
    }
}
