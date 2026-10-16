using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Identity.Features.LogIn;

internal static class LogInEndpoint
{
    public const string RateLimitPolicy = "login";

    public static void MapLogIn(this IEndpointRouteBuilder identity) =>
        identity.MapPost("/login", LogIn)
            .RequireRateLimiting(RateLimitPolicy)
            .WithSummary("Logs in with e-mail and password.")
            .WithDescription("Returns a 15-minute access token and a 7-day refresh token. Five wrong passwords lock the account for 15 minutes. Attempts are rate limited per client IP.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> LogIn(
        LogInRequest request,
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
