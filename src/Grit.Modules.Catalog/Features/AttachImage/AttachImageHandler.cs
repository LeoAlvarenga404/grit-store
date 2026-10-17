using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.AttachImage;

internal sealed class AttachImageHandler(CatalogDbContext db, ProductImageStorage storage)
    : IRequestHandler<AttachImageCommand, Result<ProductVersionResponse>>
{
    public async Task<Result<ProductVersionResponse>> Handle(AttachImageCommand command, CancellationToken cancellationToken)
    {
        var id = new ProductId(command.ProductId);
        var product = await db.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        var uploadPrefix = ProductImageStorage.UploadKeyPrefix(id);
        if (!command.Key.StartsWith(uploadPrefix, StringComparison.Ordinal))
        {
            return CatalogErrors.InvalidImageKey;
        }

        var key = $"{Product.ImageKeyPrefix(id)}{command.Key[uploadPrefix.Length..]}";

        // A repeated confirm changes nothing, not even the stored file.
        if (product.ImageKeys.Contains(key))
        {
            return new ProductVersionResponse(db.VersionOf(product));
        }

        var added = product.AddImage(key);
        if (added.IsFailure)
        {
            return added.Error;
        }

        var published = await PublishAsync(command.Key, key, cancellationToken);
        if (published.IsFailure)
        {
            return published.Error;
        }

        var version = await db.SaveProductAsync(product, command.Version, cancellationToken);
        if (version.IsFailure)
        {
            return version.Error;
        }

        await storage.DeleteAsync(command.Key, cancellationToken);
        return new ProductVersionResponse(version.Value);
    }

    // A presigned PUT cannot cap the size, so the confirm checks what the storage really holds before copying it out of reach.
    private async Task<Result> PublishAsync(string uploadKey, string key, CancellationToken cancellationToken)
    {
        var stored = await storage.FindAsync(uploadKey, cancellationToken);
        if (stored is null)
        {
            return CatalogErrors.ImageNotUploaded;
        }

        if (ImageRule.IsAllowed(stored.Value.ContentType, stored.Value.SizeInBytes)
            && await storage.CopyIfUnchangedAsync(uploadKey, key, stored.Value.ETag, cancellationToken))
        {
            return Result.Success();
        }

        await storage.DeleteAsync(uploadKey, cancellationToken);
        return CatalogErrors.InvalidImage;
    }
}
