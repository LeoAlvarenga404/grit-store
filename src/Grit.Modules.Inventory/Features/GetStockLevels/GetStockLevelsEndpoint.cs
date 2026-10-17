using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Inventory.Features.GetStockLevels;

internal static class GetStockLevelsEndpoint
{
    public static void MapGetStockLevels(this IEndpointRouteBuilder stock) =>
        stock.MapGet("/", GetStockLevels)
            .WithSummary("Gets the stock of the given SKUs.")
            .WithDescription("Repeat skuId (at most 100). A SKU without stock has no row. held counts the units of committed or unexpired reservations; available is onHand minus held.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

    private static async Task<Results<Ok<StockLevelsResponse>, ProblemHttpResult>> GetStockLevels(
        [FromQuery(Name = "skuId")] Guid[]? skuIds,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStockLevelsQuery(skuIds ?? []), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
