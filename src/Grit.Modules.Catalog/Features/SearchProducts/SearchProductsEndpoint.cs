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
                "q finds words with or without accents, in the singular or the plural, and name words with a small typo (longer words tolerate more). "
                + "attribute=key:value can repeat (one per key) and matches a single SKU. "
                + "sort is relevance (the default: by relevance with q, newest first without), price_asc or price_desc (by priceAmount, ties by id). "
                + "priceAmount is the lowest price among the SKUs that match; hasPriceRange tells whether those SKUs have different prices. "
                + "imageUrl is a signed URL of the product's first image (the cover), valid for an hour, or null. "
                + "colors lists every colour of the product's SKUs (the cor attribute): colours with images first, in the order of their first image, then the others by name; each imageUrl is that colour's first image or null. "
                + "Pass nextCursor as after, with the same filters and sort, to read the next page; drop after whenever the filters or the sort change (only its syntax is checked).")
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
            string.IsNullOrWhiteSpace(request.Sort) ? ProductSort.Relevance : request.Sort,
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
