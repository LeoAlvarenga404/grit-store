using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class AdminAuthorizationTests(ApiFactory factory)
{
    // Covers every admin route of every module, including the ones later sub-plans add.
    [Fact]
    public void AdminEndpoints_All_RequireTheAdminRole()
    {
        var adminEndpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText!.StartsWith("/api/v1/admin", StringComparison.Ordinal))
            .ToList();

        adminEndpoints.ShouldNotBeEmpty();
        adminEndpoints
            .Where(endpoint => !endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>()
                .SelectMany(policy => policy.Requirements)
                .OfType<RolesAuthorizationRequirement>()
                .Any(requirement => requirement.AllowedRoles.Contains("admin")))
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task AdminRoute_WithoutToken_Returns401()
    {
        var response = await CreateCategoryAsync(factory.CreateClient());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminRoute_CustomerToken_Returns403()
    {
        var response = await CreateCategoryAsync(CustomerClient.Create(factory, Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminRoute_AdminToken_CreatesTheCategory()
    {
        var response = await CreateCategoryAsync(AdminClient.Create(factory));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private static Task<HttpResponseMessage> CreateCategoryAsync(HttpClient client) =>
        client.PostAsJsonAsync(CatalogAdminApi.CategoriesPath, new { name = CatalogAdminApi.UniqueName("Categoria") }, TestContext.Current.CancellationToken);
}
