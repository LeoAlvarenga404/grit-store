using System.Security.Claims;
using Grit.Modules.Identity.Domain;
using Grit.Modules.Identity.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Grit.Modules.Identity.Infrastructure;

internal sealed class TokenIssuer(UsersDbContext db, UserManager<User> users, IOptions<JwtOptions> jwt)
{
    private static readonly JsonWebTokenHandler Handler = new();

    public async Task<TokenResponse> IssueAsync(User user, Guid familyId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var accessTokenExpiresAt = now + JwtOptions.AccessTokenLifetime;
        var accessToken = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = JwtOptions.Issuer,
            Audience = JwtOptions.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = accessTokenExpiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(await ClaimsOfAsync(user)),
            SigningCredentials = new SigningCredentials(jwt.Value.Key(), SecurityAlgorithms.HmacSha256),
        });

        var (refreshToken, refreshTokenValue) = RefreshToken.Issue(user.Id, familyId, now);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        return new TokenResponse(accessToken, accessTokenExpiresAt, refreshTokenValue, refreshToken.ExpiresAt);
    }

    private async Task<List<Claim>> ClaimsOfAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
        };
        claims.AddRange((await users.GetRolesAsync(user)).Select(role => new Claim(JwtOptions.RoleClaim, role)));
        return claims;
    }
}
