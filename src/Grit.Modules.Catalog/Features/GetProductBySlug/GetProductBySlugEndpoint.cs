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
            .WithDescription("A product without SKUs is not for sale yet and answers 404, as does a slug in any other spelling. imageUrls is the whole gallery, cover first; colors lists every colour of the SKUs (the cor attribute), colours with images first in the order of their first image, then the others by name, each with its own image URLs. Image URLs are signed and valid for an hour.")
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
