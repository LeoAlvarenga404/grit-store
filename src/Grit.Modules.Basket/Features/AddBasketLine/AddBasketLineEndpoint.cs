using System.Security.Claims;
using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Basket.Features.AddBasketLine;

internal static class AddBasketLineEndpoint
{
    public static void MapAddBasketLine(this IEndpointRouteBuilder basket) =>
        basket.MapPost("/lines", AddBasketLine)
            .WithSummary("Adds a SKU to the basket.")
            .WithDescription("Adding a SKU already in the basket sums the quantities (at most 10 per item) and keeps today's price.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<NoContent, ProblemHttpResult>> AddBasketLine(
        AddBasketLineRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var customerId = user.GetUserId() ?? throw new InvalidOperationException("An authorized request always carries the sub claim.");
        var result = await sender.Send(new AddBasketLineCommand(customerId, request.SkuId, request.Quantity), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.NoContent();
    }
}
