using System.Net;
using System.Security.Cryptography;
using System.Text;
using Grit.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Grit.IntegrationTests;

public class IdentityLogInTests(ApiFactory factory)
{
    private const string WrongPassword = "senha-errada-123";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task LogIn_ValidCredentials_ReturnsAnAccessTokenValidFor15Minutes()
    {
        var (id, email) = await IdentityApi.RegisterAsync(_client);

        var tokens = await IdentityApi.LogInAsync(_client, email);

        var accessToken = new JsonWebToken(tokens.GetProperty("accessToken").GetString());
        accessToken.Subject.ShouldBe(id.ToString());
        accessToken.GetClaim("email").Value.ShouldBe(email);
        accessToken.Claims.Where(claim => claim.Type == "role").Select(claim => claim.Value).ShouldHaveSingleItem().ShouldBe("customer");
        accessToken.Issuer.ShouldBe("grit-api");
        accessToken.Audiences.ShouldHaveSingleItem().ShouldBe("grit-web");
        (accessToken.ValidTo - accessToken.IssuedAt).ShouldBe(TimeSpan.FromMinutes(15));
        tokens.GetProperty("accessTokenExpiresAt").GetDateTimeOffset().UtcDateTime.ShouldBe(accessToken.ValidTo, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task LogIn_ValidCredentials_StoresOnlyTheHashOfTheRefreshToken()
    {
        var (id, email) = await IdentityApi.RegisterAsync(_client);

        var tokens = await IdentityApi.LogInAsync(_client, email);

        var refreshToken = tokens.GetProperty("refreshToken").GetString()!;
        refreshToken.Length.ShouldBe(43);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        var stored = await db.RefreshTokens.SingleAsync(token => token.UserId == id, TestContext.Current.CancellationToken);
        stored.TokenHash.ShouldBe(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        (stored.ExpiresAt - stored.CreatedAt).ShouldBe(TimeSpan.FromDays(7));
        tokens.GetProperty("refreshTokenExpiresAt").GetDateTimeOffset().ShouldBe(stored.ExpiresAt, TimeSpan.FromSeconds(1));
        stored.ReplacedAt.ShouldBeNull();
        stored.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task LogIn_WrongPassword_Returns401InvalidCredentials()
    {
        var (_, email) = await IdentityApi.RegisterAsync(_client);

        var response = await IdentityApi.LogInAsync(_client, email, WrongPassword);

        await IdentityApi.AssertUnauthorizedAsync(response, "identity.invalid_credentials");
    }

    [Fact]
    public async Task LogIn_UnknownEmail_Returns401InvalidCredentials()
    {
        var response = await IdentityApi.LogInAsync(_client, IdentityApi.UniqueEmail(), IdentityApi.Password);

        await IdentityApi.AssertUnauthorizedAsync(response, "identity.invalid_credentials");
    }

    [Fact]
    public async Task LogIn_FifthWrongPassword_LocksTheAccountEvenForTheRightPassword()
    {
        var (_, email) = await IdentityApi.RegisterAsync(_client);
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            await IdentityApi.AssertUnauthorizedAsync(await IdentityApi.LogInAsync(_client, email, WrongPassword), "identity.invalid_credentials");
        }

        await IdentityApi.AssertUnauthorizedAsync(await IdentityApi.LogInAsync(_client, email, WrongPassword), "identity.locked_out");

        await IdentityApi.AssertUnauthorizedAsync(await IdentityApi.LogInAsync(_client, email, IdentityApi.Password), "identity.locked_out");
    }

    [Theory]
    [InlineData("", "senha-de-teste-123", "email")]
    [InlineData("ana\0@grit.dev", "senha-de-teste-123", "email")]
    [InlineData("ana@grit.dev", "", "password")]
    public async Task LogIn_InvalidInput_Returns400WithTheField(string email, string password, string field)
    {
        var response = await IdentityApi.LogInAsync(_client, email, password);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").TryGetProperty(field, out _).ShouldBeTrue();
    }

    // A host of its own: the limiter is per host, and the shared host allows 1000 attempts a minute.
    [Fact]
    public async Task LogIn_MoreAttemptsPerMinuteThanTheLimit_Returns429()
    {
        using var limited = factory.WithWebHostBuilder(builder => builder.UseSetting("Identity:LoginAttemptsPerMinute", "2"));
        var client = limited.CreateClient();

        (await IdentityApi.LogInAsync(client, IdentityApi.UniqueEmail(), IdentityApi.Password)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await IdentityApi.LogInAsync(client, IdentityApi.UniqueEmail(), IdentityApi.Password)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        (await IdentityApi.LogInAsync(client, IdentityApi.UniqueEmail(), IdentityApi.Password)).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
