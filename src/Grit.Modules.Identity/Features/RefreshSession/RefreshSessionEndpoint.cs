using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Identity.Features.RefreshSession;

internal static class RefreshSessionEndpoint
{
    public static void MapRefreshSession(this IEndpointRouteBuilder identity) =>
        identity.MapPost("/refresh", RefreshSession)
            .WithSummary("Trades a refresh token for a new token pair.")
            .WithDescription("Each refresh token works once. Presenting one that was already traded ends every session started from the same login. Send one refresh at a time per session.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> RefreshSession(
        RefreshSessionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
