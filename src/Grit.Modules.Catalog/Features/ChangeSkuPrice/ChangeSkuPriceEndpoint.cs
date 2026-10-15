using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.ChangeSkuPrice;

internal static class ChangeSkuPriceEndpoint
{
    public static void MapChangeSkuPrice(this IEndpointRouteBuilder products) =>
        products.MapPut("/{id:guid}/skus/{skuId:guid}/price", ChangeSkuPrice)
            .WithSummary("Changes the price of one SKU.")
            .WithDescription("Send the product version last read; a stale version answers 409 catalog.product_changed. Prices are in BRL.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<ProductVersionResponse>, ProblemHttpResult>> ChangeSkuPrice(
        Guid id,
        Guid skuId,
        ChangeSkuPriceRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ChangeSkuPriceCommand(id, skuId, request.PriceAmount, request.PriceCurrency, request.Version);
        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
