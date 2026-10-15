using System.Net;
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
}
