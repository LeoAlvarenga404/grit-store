using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Ordering.Features.ListAllOrders;

internal static class ListAllOrdersEndpoint
{
    public const int DefaultLimit = 50;

    public static void MapListAllOrders(this IEndpointRouteBuilder orders) =>
        orders.MapGet("/", ListAllOrders)
            .WithSummary("Lists every customer's orders, newest first.")
            .WithDescription("status filters by Placed, Confirmed, Cancelled or Shipped. limit is 1 to 100 (default 50). Pass nextCursor as after to read the next page.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

    private static async Task<Results<Ok<ListAllOrdersResponse>, ProblemHttpResult>> ListAllOrders(
        [AsParameters] ListAllOrdersRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new ListAllOrdersQuery(
            string.IsNullOrWhiteSpace(request.Status) ? null : request.Status,
            string.IsNullOrWhiteSpace(request.After) ? null : request.After,
            request.Limit ?? DefaultLimit);
        var result = await sender.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
