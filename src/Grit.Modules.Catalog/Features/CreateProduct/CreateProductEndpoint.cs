using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.CreateProduct;

internal static class CreateProductEndpoint
{
    public static void MapCreateProduct(this IEndpointRouteBuilder products) =>
        products.MapPost("/", CreateProduct)
            .WithSummary("Creates a product without SKUs.")
            .WithDescription("The slug comes from the name and never changes. The response carries the version to send on the next change.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<CreateProductResponse>, ProblemHttpResult>> CreateProduct(
        CreateProductRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(request.CategoryId, request.Name, request.Description, request.Attributes ?? []);
        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Created($"/api/v1/admin/products/{result.Value.Id}", result.Value);
    }
}
