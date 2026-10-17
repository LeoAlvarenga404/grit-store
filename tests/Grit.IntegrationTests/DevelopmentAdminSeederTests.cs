using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Grit.IntegrationTests;

public class DevelopmentAdminSeederTests(ApiFactory api)
{
    private const string Password = "senha-do-admin-123";

    [Fact]
    public async Task DevelopmentStartup_TwiceWithAdminPassword_SeedsOneAdminWhoCanUseAdminRoutes()
    {
        var email = UniqueEmail();
        await using (var first = CreateHost(Environments.Development, email))
        {
            _ = first.Services;
        }

        await using var second = CreateHost(Environments.Development, email);
        var client = second.CreateClient();
        var tokens = await IdentityApi.ReadOkAsync(await IdentityApi.LogInAsync(client, email, Password));
        var accessToken = tokens.GetProperty("accessToken").GetString()!;

        var me = await IdentityApi.ReadOkAsync(await IdentityApi.GetMeAsync(client, accessToken));
        string?[] roles = [.. me.GetProperty("roles").EnumerateArray().Select(role => role.GetString())];
        roles.ShouldBe(["admin"]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.PostAsJsonAsync(CatalogAdminApi.CategoriesPath, new { name = CatalogAdminApi.UniqueName("Categoria") }, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ProductionStartup_WithAdminPassword_SeedsNoAdmin()
    {
        var email = UniqueEmail();
        await using var production = CreateHost(Environments.Production, email);

        var response = await IdentityApi.LogInAsync(production.CreateClient(), email, Password);

        await IdentityApi.AssertUnauthorizedAsync(response, "identity.invalid_credentials");
    }

    private static string UniqueEmail() => $"admin-{Guid.NewGuid():N}@grit.local";

    private WebApplicationFactory<Program> CreateHost(string environment, string email) =>
        api.WithWebHostBuilder(builder => builder
            .UseEnvironment(environment)
            .UseSetting("Identity:Admin:Email", email)
            .UseSetting("Identity:Admin:Password", Password));
}
