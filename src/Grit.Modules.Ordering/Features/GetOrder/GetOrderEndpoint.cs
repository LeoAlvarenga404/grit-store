using System.Security.Claims;
using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Ordering.Features.GetOrder;

internal static class GetOrderEndpoint
{
    public static void MapGetOrder(this IEndpointRouteBuilder orders) =>
        orders.MapGet("/{id:guid}", GetOrder)
            .WithSummary("Gets one of the customer's orders with its lines, totals and status timeline.")
            .WithDescription("Another customer's order answers 404. Each line keeps the product slug, colour and size bought (null on orders placed before they were kept); imageUrl is read from the catalog on each request: the first image of the SKU's colour (for a SKU without colour, the first image without colour), signed for an hour; null when there is none or the SKU is gone.")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<OrderResponse>, ProblemHttpResult>> GetOrder(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var customerId = user.GetUserId() ?? throw new InvalidOperationException("An authorized request always carries the sub claim.");
        var result = await sender.Send(new GetOrderQuery(customerId, id), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
