using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.CreateCategory;

internal static class CreateCategoryEndpoint
{
    public static void MapCreateCategory(this IEndpointRouteBuilder admin) =>
        admin.MapPost("/categories", CreateCategory)
            .WithSummary("Creates a category.")
            .WithDescription("The slug comes from the name; two categories cannot share a slug.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

    // No Location header: categories are read through the catalog listing (sub-plan 1b), not by id.
    private static async Task<Results<Created<CreateCategoryResponse>, ProblemHttpResult>> CreateCategory(
        CreateCategoryRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCategoryCommand(request.Name), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Created((string?)null, result.Value);
    }
}
