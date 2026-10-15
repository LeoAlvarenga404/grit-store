using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.UpdateProduct;

internal static class UpdateProductEndpoint
{
    public static void MapUpdateProduct(this IEndpointRouteBuilder products) =>
        products.MapPut("/{id:guid}", UpdateProduct)
            .WithSummary("Updates a product's category, name, description and attributes.")
            .WithDescription("Send the version last read; a stale version answers 409 catalog.product_changed. The slug does not change.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<ProductVersionResponse>, ProblemHttpResult>> UpdateProduct(
        Guid id,
        UpdateProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            id, request.CategoryId, request.Name, request.Description, request.Attributes ?? [], request.Version);
        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
