using System.Net;

namespace Grit.IntegrationTests;

public class ProblemDetailsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task UnhandledException_Returns500WithoutExceptionDetails()
    {
        var response = await _client.GetAsync("/api/v1/probe/fail", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.InternalServerError);
        problem.GetRawText().ShouldNotContain("secret");
        problem.GetRawText().ShouldNotContain(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task UnknownRoute_Returns404Problem()
    {
        var response = await _client.GetAsync("/api/v1/nothing-here", TestContext.Current.CancellationToken);

        await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
    }
}
