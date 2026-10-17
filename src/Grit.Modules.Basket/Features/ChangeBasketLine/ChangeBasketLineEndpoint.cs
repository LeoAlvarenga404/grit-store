using System.Security.Claims;
using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Basket.Features.ChangeBasketLine;

internal static class ChangeBasketLineEndpoint
{
    public static void MapChangeBasketLine(this IEndpointRouteBuilder basket) =>
        basket.MapPut("/lines/{skuId:guid}", ChangeBasketLine)
            .WithSummary("Changes the quantity of a basket item.")
            .WithDescription("Quantity 1 to 10; to remove an item use DELETE.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<NoContent, ProblemHttpResult>> ChangeBasketLine(
        Guid skuId,
        ChangeBasketLineRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var customerId = user.GetUserId() ?? throw new InvalidOperationException("An authorized request always carries the sub claim.");
        var result = await sender.Send(new ChangeBasketLineCommand(customerId, skuId, request.Quantity), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.NoContent();
    }
}
