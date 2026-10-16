using System.Security.Claims;

namespace Grit.BuildingBlocks;

public static class ClaimsPrincipalExtensions
{
    // Identity issues the user id as the JWT "sub" claim, and the API keeps JWT claim names (MapInboundClaims = false).
    public const string UserIdClaim = "sub";

    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return Guid.TryParse(user.FindFirstValue(UserIdClaim), out var id) ? id : null;
    }
}
