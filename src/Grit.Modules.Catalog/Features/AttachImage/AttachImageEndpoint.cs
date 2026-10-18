using Grit.BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Catalog.Features.AttachImage;

internal static class AttachImageEndpoint
{
    public static void MapAttachImage(this IEndpointRouteBuilder products) =>
        products.MapPost("/{id:guid}/images", AttachImage)
            .WithSummary("Attaches an uploaded image to the product, after its other images.")
            .WithDescription("Send the key from /images/uploads, the colour the image shows (the cor value of the coloured SKU it shows; leave it out, or send null, only for an image of a SKU without cor) and the product version last read. The stored file is checked again: a wrong type or a file over 2 MB is deleted (400 catalog.invalid_image). A colour no SKU has is 400 catalog.image_color_not_found; a colour has at most 8 images (409 catalog.image_limit). The same key again with the same colour answers the current version; with another colour it is 409 catalog.duplicate_image. A new image sent with a stale version is 409 catalog.product_changed.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<ProductVersionResponse>, ProblemHttpResult>> AttachImage(
        Guid id,
        AttachImageRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AttachImageCommand(id, request.Key, request.Color, request.Version), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return TypedResults.Ok(result.Value);
    }
}
