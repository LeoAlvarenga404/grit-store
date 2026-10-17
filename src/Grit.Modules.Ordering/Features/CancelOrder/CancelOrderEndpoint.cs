using System.Security.Claims;
using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Ordering.Features.CancelOrder;

internal static class CancelOrderEndpoint
{
    public static void MapCancelOrder(this IEndpointRouteBuilder orders) =>
        orders.MapPost("/{id:guid}/cancel", CancelOrder)
            .WithSummary("Cancels a confirmed order: the stock is released and the payment refunded.")
            .WithDescription("Only Confirmed orders can be cancelled (409 ordering.order_not_cancellable otherwise).")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelOrder(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var customerId = user.GetUserId() ?? throw new InvalidOperationException("An authorized request always carries the sub claim.");
        var result = await sender.Send(new CancelOrderCommand(customerId, id), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.NoContent();
    }
}
