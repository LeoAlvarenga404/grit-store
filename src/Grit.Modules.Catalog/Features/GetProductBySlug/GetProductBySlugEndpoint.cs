using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.GetProductBySlug;

internal static class GetProductBySlugEndpoint
{
    public static void MapGetProductBySlug(this IEndpointRouteBuilder catalog) =>
        catalog.MapGet("/products/{slug}", GetProductBySlug)
            .WithSummary("Gets a product page by slug: category, attributes and SKUs with prices.")
            .WithDescription("A product without SKUs is not for sale yet and answers 404, as does a slug in any other spelling.")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<GetProductBySlugResponse>, ProblemHttpResult>> GetProductBySlug(
        string slug,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductBySlugQuery(slug), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
