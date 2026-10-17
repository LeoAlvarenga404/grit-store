using System.Security.Claims;

namespace Grit.BuildingBlocks;

public static class ClaimsPrincipalExtensions
{
    // Identity issues the user id as the JWT "sub" claim, and the API keeps JWT claim names (MapInboundClaims = false).
    public const string UserIdClaim = "sub";

    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        // A "sub" claim on an anonymous identity does not make a user: only authenticated identities count.
        var userIdValue = user.Identities
            .Where(identity => identity.IsAuthenticated)
            .Select(identity => identity.FindFirst(UserIdClaim)?.Value)
            .FirstOrDefault(value => value is not null);
        return Guid.TryParse(userIdValue, out var id) ? id : null;
    }
}
