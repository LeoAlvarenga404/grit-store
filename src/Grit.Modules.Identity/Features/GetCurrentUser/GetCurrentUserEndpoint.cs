using System.Security.Claims;
using Grit.BuildingBlocks;
using Grit.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Grit.Modules.Identity.Features.GetCurrentUser;

internal static class GetCurrentUserEndpoint
{
    public static void MapGetCurrentUser(this IEndpointRouteBuilder identity) =>
        identity.MapGet("/me", GetCurrentUser)
            .RequireAuthorization()
            .WithSummary("Gets the logged-in user.")
            .WithDescription("Read from the access token, without a database read: a role change shows after the next refresh.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

    private static Ok<CurrentUserResponse> GetCurrentUser(ClaimsPrincipal user) =>
        TypedResults.Ok(new CurrentUserResponse(
            user.GetUserId() ?? throw new InvalidOperationException("An authorized request always carries the sub claim."),
            user.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty,
            [.. user.FindAll(JwtOptions.RoleClaim).Select(claim => claim.Value)]));
}
