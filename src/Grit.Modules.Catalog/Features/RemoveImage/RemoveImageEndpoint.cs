using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.RemoveImage;

internal static class RemoveImageEndpoint
{
    public static void MapRemoveImage(this IEndpointRouteBuilder products) =>
        products.MapDelete("/{id:guid}/images/{fileName}", RemoveImage)
            .WithSummary("Removes an image from the product.")
            .WithDescription("fileName is the last segment of the image key; version is the product version last read.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<ProductVersionResponse>, ProblemHttpResult>> RemoveImage(
        Guid id,
        string fileName,
        uint version,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveImageCommand(id, fileName, version), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
