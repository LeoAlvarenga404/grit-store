using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Identity.Features.LogOut;

internal static class LogOutEndpoint
{
    public static void MapLogOut(this IEndpointRouteBuilder identity) =>
        identity.MapPost("/logout", LogOut)
            .WithSummary("Ends the session of a refresh token.")
            .WithDescription("Revokes the refresh token and every token rotated from the same login; other logins of the user keep working. Answers 204 for unknown or already revoked tokens too. An access token already issued stays valid until it expires (15 minutes).")
            .ProducesProblem(StatusCodes.Status400BadRequest);

    private static async Task<Results<NoContent, ProblemHttpResult>> LogOut(
        LogOutRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.NoContent();
    }
}
