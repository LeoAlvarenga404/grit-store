using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace Grit.IntegrationTests;

public class OpenApiTests(ApiFactory factory)
{
    [Fact]
    public async Task OpenApiDocument_Development_ListsHealthAndModuleEndpoints()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var document = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        document.ShouldContain("\"/health\"");
        document.ShouldContain("\"/api/v1/probe/notes\"");
        document.ShouldContain("\"Probe\"");
        document.ShouldContain("\"/api/v1/admin/products\"");
        document.ShouldContain("\"Catalog\"");
        document.ShouldContain("\"/api/v1/catalog/products\"");
        document.ShouldContain("\"/api/v1/catalog/facets\"");
        document.ShouldContain("\"/api/v1/catalog/suggestions\"");
        document.ShouldContain("\"/api/v1/catalog/products/{slug}\"");
        document.ShouldContain("\"Identity\"");
        document.ShouldContain("\"/api/v1/identity/register\"");
        document.ShouldContain("\"/api/v1/identity/login\"");
        document.ShouldContain("\"/api/v1/identity/refresh\"");
        document.ShouldContain("\"/api/v1/identity/logout\"");
        document.ShouldContain("\"/api/v1/identity/me\"");
    }

    [Fact]
    public async Task OpenApiDocument_Development_DeclaresTheErrorCodeAndTheBearerScheme()
    {
        var document = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json", TestContext.Current.CancellationToken);

        var components = document.GetProperty("components");
        var problem = components.GetProperty("schemas").GetProperty("ProblemDetails").GetProperty("properties");
        problem.GetProperty("code").GetProperty("type").GetString().ShouldBe("string");
        problem.GetProperty("errors").GetProperty("additionalProperties").GetProperty("items").GetProperty("type").GetString().ShouldBe("string");
        var bearer = components.GetProperty("securitySchemes").GetProperty("Bearer");
        bearer.GetProperty("type").GetString().ShouldBe("http");
        bearer.GetProperty("scheme").GetString().ShouldBe("bearer");
        var paths = document.GetProperty("paths");
        SecuritySchemeOf(paths, "/api/v1/identity/me", "get").ShouldBe("Bearer");
        SecuritySchemeOf(paths, "/api/v1/admin/products", "post").ShouldBe("Bearer");
        paths.GetProperty("/api/v1/catalog/products").GetProperty("get").TryGetProperty("security", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    public async Task DevelopmentOnlyEndpoints_Production_ReturnNotFound(string path)
    {
        using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var response = await production.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static string SecuritySchemeOf(JsonElement paths, string path, string method) =>
        paths.GetProperty(path).GetProperty(method).GetProperty("security")[0].EnumerateObject().Single().Name;
}
