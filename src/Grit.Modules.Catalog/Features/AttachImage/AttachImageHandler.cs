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

        var added = product.AddImage(command.Key);
        if (added.IsFailure)
        {
            return added.Error;
        }

        // A presigned PUT cannot cap the size, so the confirm checks what the storage really holds.
        var stored = await storage.FindAsync(command.Key, cancellationToken);
        if (stored is null)
        {
            return CatalogErrors.ImageNotUploaded;
        }

        if (!ImageRule.IsAllowed(stored.Value.ContentType, stored.Value.SizeInBytes))
        {
            await storage.DeleteAsync(command.Key, cancellationToken);
            return CatalogErrors.InvalidImage;
        }

        var version = await db.SaveProductAsync(product, command.Version, cancellationToken);
        if (version.IsFailure)
        {
            return version.Error;
        }

        return new ProductVersionResponse(version.Value);
    }
}
