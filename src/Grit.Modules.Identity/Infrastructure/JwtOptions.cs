using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Grit.Modules.Identity.Infrastructure;

internal sealed class JwtOptions
{
    public const string Section = "Identity:Jwt";

    public const string Issuer = "grit-api";

    public const string Audience = "grit-web";

    public const string RoleClaim = "role";

    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);

    private const int MinimumKeyLengthInBytes = 32;

    public string SigningKey { get; set; } = string.Empty;

    // Read on use, never at registration: the build-time OpenAPI run composes the modules without configuration (ADR 0004).
    public SymmetricSecurityKey Key()
    {
        var bytes = Encoding.UTF8.GetBytes(SigningKey);
        if (bytes.Length < MinimumKeyLengthInBytes)
        {
            throw new InvalidOperationException($"{Section}:SigningKey must have at least {MinimumKeyLengthInBytes} bytes (HS256).");
        }

        return new SymmetricSecurityKey(bytes);
    }

    public void ConfigureBearer(JwtBearerOptions bearer)
    {
        // Claims keep their JWT names ("sub", "role"), which ClaimsPrincipalExtensions.GetUserId reads.
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = Key(),
            NameClaimType = JwtRegisteredClaimNames.Email,
            RoleClaimType = RoleClaim,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }
}
