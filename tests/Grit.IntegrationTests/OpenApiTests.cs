using System.Net;

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
}
