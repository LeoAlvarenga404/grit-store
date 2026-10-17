using System.Net.Http.Headers;
using System.Text;
using Grit.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Grit.IntegrationTests;

internal static class CustomerClient
{
    private static readonly JsonWebTokenHandler Handler = new();

    public static HttpClient Create(WebApplicationFactory<Program> factory, Guid customerId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(customerId, "customer"));
        return client;
    }

    // Signed like Identity's tokens with the test host's key, so these requests take the production bearer path.
    public static string Token(Guid userId, string role) =>
        Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = JwtOptions.Issuer,
            Audience = JwtOptions.Audience,
            Expires = DateTime.UtcNow.AddMinutes(15),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                [JwtOptions.RoleClaim] = role,
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiFactory.JwtSigningKey)),
                SecurityAlgorithms.HmacSha256),
        });
}
