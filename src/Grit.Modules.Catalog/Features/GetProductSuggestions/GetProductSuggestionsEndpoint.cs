using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.GetProductSuggestions;

internal static class GetProductSuggestionsEndpoint
{
    public static void MapGetProductSuggestions(this IEndpointRouteBuilder catalog) =>
        catalog.MapGet("/suggestions", GetProductSuggestions)
            .WithSummary("Suggests up to 8 product names for the text typed so far.")
            .WithDescription("q takes 2 to 50 characters. Accents and case are ignored, and names that start with q come first.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

    private static async Task<Results<Ok<IReadOnlyList<ProductSuggestionResponse>>, ProblemHttpResult>> GetProductSuggestions(
        [FromQuery(Name = "q")] string? query,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductSuggestionsQuery(query?.Trim()), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
