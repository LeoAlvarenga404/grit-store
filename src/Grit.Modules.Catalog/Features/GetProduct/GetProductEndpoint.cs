using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.GetProduct;

internal static class GetProductEndpoint
{
    public static void MapGetProduct(this IEndpointRouteBuilder products) =>
        products.MapGet("/{id:guid}", GetProduct)
            .WithSummary("Gets a product with its SKUs and current version.")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<GetProductResponse>, ProblemHttpResult>> GetProduct(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
