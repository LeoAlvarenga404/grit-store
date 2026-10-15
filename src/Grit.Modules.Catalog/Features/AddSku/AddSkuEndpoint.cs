using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.AddSku;

internal static class AddSkuEndpoint
{
    public static void MapAddSku(this IEndpointRouteBuilder products) =>
        products.MapPost("/{id:guid}/skus", AddSku)
            .WithSummary("Adds a SKU (a variant such as color and size) with its price to a product.")
            .WithDescription("Send the product version last read; a stale version answers 409 catalog.product_changed. Prices are in BRL.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<AddSkuResponse>, ProblemHttpResult>> AddSku(
        Guid id,
        AddSkuRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AddSkuCommand(id, request.Attributes ?? [], request.PriceAmount, request.PriceCurrency, request.Version);
        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Created($"/api/v1/admin/products/{id}", result.Value);
    }
}
