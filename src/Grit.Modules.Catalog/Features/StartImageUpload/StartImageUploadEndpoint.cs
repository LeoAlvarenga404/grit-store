using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.StartImageUpload;

internal static class StartImageUploadEndpoint
{
    public static void MapStartImageUpload(this IEndpointRouteBuilder products) =>
        products.MapPost("/{id:guid}/images/uploads", StartImageUpload)
            .WithSummary("Starts a product image upload: returns a presigned PUT URL.")
            .WithDescription("JPEG, PNG or WebP up to 2 MB. PUT the file to uploadUrl with the same Content-Type before expiresAt (5 minutes), then attach it with POST /images and the key.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Results<Ok<ImageUploadResponse>, ProblemHttpResult>> StartImageUpload(
        Guid id,
        StartImageUploadRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartImageUploadCommand(id, request.ContentType, request.SizeInBytes), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
