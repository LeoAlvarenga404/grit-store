using Grit.BuildingBlocks;
using Grit.Modules.Catalog.Contracts;
using Grit.Modules.Inventory.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Inventory.Features.SetStock;

internal static class SetStockEndpoint
{
    public static void MapSetStock(this IEndpointRouteBuilder stock) =>
        stock.MapPut("/{skuId:guid}", SetStock)
            .WithSummary("Sets the on-hand units of a SKU.")
            .WithDescription("Creates the stock of a SKU that has none. onHand cannot drop below the units held by reservations (409 inventory.below_held_units).")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    // The catalog read opens its own connection, so it runs before the command's transaction (ADR 0005).
    private static async Task<Results<NoContent, ProblemHttpResult>> SetStock(
        Guid skuId,
        SetStockRequest request,
        ICatalogReader catalog,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if ((await catalog.GetSkusAsync([skuId], cancellationToken)).Count == 0)
        {
            return InventoryErrors.SkuNotFound.ToProblem();
        }

        var result = await sender.Send(new SetStockCommand(skuId, request.OnHand), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.NoContent();
    }
}
