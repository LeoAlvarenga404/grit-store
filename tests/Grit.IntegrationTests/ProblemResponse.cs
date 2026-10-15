using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

internal static class ProblemResponse
{
    public static async Task<JsonElement> ReadAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        response.StatusCode.ShouldBe(expectedStatus);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        problem.GetProperty("status").GetInt32().ShouldBe((int)expectedStatus);
        return problem;
    }
}
