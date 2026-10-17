using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.SearchProducts;

internal static class SearchProductsEndpoint
{
    public const int DefaultLimit = 20;

    public static void MapSearchProducts(this IEndpointRouteBuilder catalog) =>
        catalog.MapGet("/products", SearchProducts)
            .WithSummary("Searches and lists the products that have at least one SKU.")
            .WithDescription(
                "q finds words with or without accents, in the singular or the plural, and name words with a small typo (longer words tolerate more); results are ordered by relevance. "
                + "Without q, the newest products come first (creation time, to the millisecond). attribute=key:value can repeat (one per key) and matches a single SKU. "
                + "priceAmount is the lowest price among the SKUs that match."
                + " imageUrl is a signed URL of the product's first image, valid for an hour, or null."
                + " Pass nextCursor as after to read the next page.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

    private static async Task<Results<Ok<SearchProductsResponse>, ProblemHttpResult>> SearchProducts(
        [AsParameters] SearchProductsRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new SearchProductsQuery(
            string.IsNullOrWhiteSpace(request.Query) ? null : request.Query.Trim(),
            string.IsNullOrWhiteSpace(request.Category) ? null : request.Category,
            request.MinPrice,
            request.MaxPrice,
            request.Attributes ?? [],
            string.IsNullOrWhiteSpace(request.After) ? null : request.After,
            request.Limit ?? DefaultLimit);
        var result = await sender.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
