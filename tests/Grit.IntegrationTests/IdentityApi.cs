using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

internal static class IdentityApi
{
    public const string RegisterPath = "/api/v1/identity/register";

    public const string LogInPath = "/api/v1/identity/login";

    public const string Password = "senha-de-teste-123";

    public static string UniqueEmail() => $"cliente-{Guid.NewGuid():N}@grit.dev";

    public static async Task<(Guid Id, string Email)> RegisterAsync(HttpClient client)
    {
        var email = UniqueEmail();
        var response = await client.PostAsJsonAsync(RegisterPath, new { email, password = Password }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return ((await CatalogAdminApi.ReadAsync(response)).GetProperty("id").GetGuid(), email);
    }

    public static Task<HttpResponseMessage> LogInAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync(LogInPath, new { email, password }, TestContext.Current.CancellationToken);

    public static async Task<JsonElement> LogInAsync(HttpClient client, string email) =>
        await ReadOkAsync(await LogInAsync(client, email, Password));

    public static async Task<JsonElement> ReadOkAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await CatalogAdminApi.ReadAsync(response);
    }

    public static async Task AssertUnauthorizedAsync(HttpResponseMessage response, string code)
    {
        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Unauthorized);
        problem.GetProperty("code").GetString().ShouldBe(code);
    }
}
