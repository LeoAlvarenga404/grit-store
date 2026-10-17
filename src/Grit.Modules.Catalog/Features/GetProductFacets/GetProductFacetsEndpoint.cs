using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.GetProductFacets;

internal static class GetProductFacetsEndpoint
{
    public static void MapGetProductFacets(this IEndpointRouteBuilder catalog) =>
        catalog.MapGet("/facets", GetProductFacets)
            .WithSummary("Counts the products of a search per category, price range and attribute value.")
            .WithDescription(
                "Takes the q, category, minPrice, maxPrice and attribute of the product search. Every count is the number of products "
                + "the search returns for that refinement. A price range is minAmount ≤ price < maxAmount (no upper bound when maxAmount is null); "
                + "to list it, send minPrice = the larger of minAmount and the current minPrice, and maxPrice = the smaller of maxAmount−0.01 "
                + "and the current maxPrice (leave out a bound that is absent on both sides).")
            .ProducesProblem(StatusCodes.Status400BadRequest);

    private static async Task<Results<Ok<GetProductFacetsResponse>, ProblemHttpResult>> GetProductFacets(
        [AsParameters] GetProductFacetsRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetProductFacetsQuery(
            string.IsNullOrWhiteSpace(request.Query) ? null : request.Query.Trim(),
            string.IsNullOrWhiteSpace(request.Category) ? null : request.Category,
            request.MinPrice,
            request.MaxPrice,
            request.Attributes ?? []);
        var result = await sender.Send(query, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
