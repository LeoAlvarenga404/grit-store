using System.Security.Claims;
using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Basket.Features.RemoveBasketLine;

internal static class RemoveBasketLineEndpoint
{
    public static void MapRemoveBasketLine(this IEndpointRouteBuilder basket) =>
        basket.MapDelete("/lines/{skuId:guid}", RemoveBasketLine)
            .WithSummary("Removes an item from the basket.")
            .WithDescription("Answers 204 also when the item is not in the basket.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

    private static async Task<NoContent> RemoveBasketLine(
        Guid skuId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var customerId = user.GetUserId() ?? throw new InvalidOperationException("An authorized request always carries the sub claim.");
        await sender.Send(new RemoveBasketLineCommand(customerId, skuId), cancellationToken);
        return TypedResults.NoContent();
    }
}
