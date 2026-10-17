using System.Security.Claims;
using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Ordering.Features.ListOrders;

internal static class ListOrdersEndpoint
{
    public static void MapListOrders(this IEndpointRouteBuilder orders) =>
        orders.MapGet("/", ListOrders)
            .WithSummary("Lists the customer's orders, newest first.")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

    private static async Task<Ok<IReadOnlyList<OrderSummaryResponse>>> ListOrders(
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var customerId = user.GetUserId() ?? throw new InvalidOperationException("An authorized request always carries the sub claim.");
        var orders = await sender.Send(new ListOrdersQuery(customerId), cancellationToken);
        return TypedResults.Ok(orders);
    }
}
