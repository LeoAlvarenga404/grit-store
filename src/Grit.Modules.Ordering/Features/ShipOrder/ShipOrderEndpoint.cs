using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Ordering.Features.ShipOrder;

internal static class ShipOrderEndpoint
{
    public static void MapShipOrder(this IEndpointRouteBuilder orders) =>
        orders.MapPost("/{id:guid}/ship", ShipOrder)
            .WithSummary("Ships a confirmed order: its reserved units leave the stock.")
            .WithDescription("Only Confirmed orders can be shipped (409 ordering.order_not_shippable otherwise); a shipped order can no longer be cancelled.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<NoContent, ProblemHttpResult>> ShipOrder(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ShipOrderCommand(id), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.NoContent();
    }
}
