using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.ListProducts;

internal static class ListProductsEndpoint
{
    public static void MapListProducts(this IEndpointRouteBuilder products) =>
        products.MapGet("/", ListProducts)
            .WithSummary("Lists every product by name, 50 per page, including products without SKUs.")
            .WithDescription("page starts at 1; hasNextPage tells whether page + 1 has products.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

    private static async Task<Results<Ok<ListProductsResponse>, ProblemHttpResult>> ListProducts(
        int? page,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListProductsQuery(page ?? 1), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
