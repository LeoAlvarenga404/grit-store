using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Grit.IntegrationTests;

public class IdentityMeTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Me_WithAccessToken_ReturnsIdEmailAndRoles()
    {
        var (id, email) = await IdentityApi.RegisterAsync(_client);
        var accessToken = (await IdentityApi.LogInAsync(_client, email)).GetProperty("accessToken").GetString()!;

        var me = await IdentityApi.ReadOkAsync(await IdentityApi.GetMeAsync(_client, accessToken));

        me.GetProperty("id").GetGuid().ShouldBe(id);
        me.GetProperty("email").GetString().ShouldBe(email);
        me.GetProperty("roles").EnumerateArray().Select(role => role.GetString()).ShouldHaveSingleItem().ShouldBe("customer");
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync(IdentityApi.MePath, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Control for the two tests below: the same hand-made token with the API's key and a future expiry is accepted.
    [Fact]
    public async Task Me_TokenSignedWithTheApiKey_Returns200()
    {
        var token = Sign(ApiFactory.JwtSigningKey, DateTime.UtcNow.AddMinutes(15));

        (await IdentityApi.GetMeAsync(_client, token)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_TokenSignedWithAnotherKey_Returns401()
    {
        var token = Sign("another-test-signing-key-0123456789abcdef", DateTime.UtcNow.AddMinutes(15));

        (await IdentityApi.GetMeAsync(_client, token)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ExpiredAccessToken_Returns401()
    {
        var token = Sign(ApiFactory.JwtSigningKey, DateTime.UtcNow.AddMinutes(-5));

        (await IdentityApi.GetMeAsync(_client, token)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Signed with the API's key and the right audience: only the issuer check can reject these.
    [Fact]
    public async Task GetMe_TokenFromAnotherIssuer_Returns401()
    {
        var token = Sign(ApiFactory.JwtSigningKey, DateTime.UtcNow.AddMinutes(15), issuer: "someone-else");

        (await IdentityApi.GetMeAsync(_client, token)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Signed with the API's key and the right issuer: only the audience check can reject this.
    [Fact]
    public async Task GetMe_TokenForAnotherAudience_Returns401()
    {
        var token = Sign(ApiFactory.JwtSigningKey, DateTime.UtcNow.AddMinutes(15), audience: "someone-else");

        (await IdentityApi.GetMeAsync(_client, token)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static string Sign(string key, DateTime expires, string issuer = "grit-api", string audience = "grit-web") =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = expires.AddMinutes(-15),
            NotBefore = expires.AddMinutes(-15),
            Expires = expires,
            Subject = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim("email", "ana@grit.dev"), new Claim("role", "customer")]),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
        });
}
