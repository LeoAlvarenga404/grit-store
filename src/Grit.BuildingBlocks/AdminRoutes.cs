using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Grit.BuildingBlocks;

public static class AdminRoutes
{
    // The role Identity grants to admins (Identity's Roles.Admin); JWT bearer reads it from the "role" claim.
    public const string Role = "admin";

    // Every module maps its admin endpoints under this group, so no admin route can be left open by mistake.
    public static RouteGroupBuilder MapAdminGroup(this IEndpointRouteBuilder api) =>
        api.MapGroup("/admin")
            .RequireAuthorization(policy => policy.RequireRole(Role))
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
}
