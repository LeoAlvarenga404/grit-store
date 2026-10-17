using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.StartImageUpload;

internal sealed class StartImageUploadHandler(CatalogDbContext db, ProductImageStorage storage)
    : IRequestHandler<StartImageUploadCommand, Result<ImageUploadResponse>>
{
    public async Task<Result<ImageUploadResponse>> Handle(StartImageUploadCommand command, CancellationToken cancellationToken)
    {
        var id = new ProductId(command.ProductId);
        if (!await db.Products.AnyAsync(product => product.Id == id, cancellationToken))
        {
            return CatalogErrors.ProductNotFound;
        }

        var key = $"{Product.ImageKeyPrefix(id)}{Guid.CreateVersion7()}.{ImageRule.ExtensionByContentType[command.ContentType]}";
        var expiresAt = DateTimeOffset.UtcNow + ProductImageStorage.UploadLifetime;
        return new ImageUploadResponse(key, await storage.UploadUrlAsync(key, command.ContentType, expiresAt), expiresAt);
    }
}
