using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

internal static class IdentityApi
{
    public const string RegisterPath = "/api/v1/identity/register";

    public const string LogInPath = "/api/v1/identity/login";

    public const string RefreshPath = "/api/v1/identity/refresh";

    public const string MePath = "/api/v1/identity/me";

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

    public static async Task<JsonElement> StartSessionAsync(HttpClient client)
    {
        var (_, email) = await RegisterAsync(client);
        return await LogInAsync(client, email);
    }

    public static string RefreshTokenOf(JsonElement tokens) => tokens.GetProperty("refreshToken").GetString()!;

    public static Task<HttpResponseMessage> RefreshAsync(HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync(RefreshPath, new { refreshToken }, TestContext.Current.CancellationToken);

    public static async Task<HttpResponseMessage> GetMeAsync(HttpClient client, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, MePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
