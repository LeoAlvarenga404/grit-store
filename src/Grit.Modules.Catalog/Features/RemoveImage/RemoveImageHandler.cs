using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.RemoveImage;

internal sealed class RemoveImageHandler(CatalogDbContext db) : IRequestHandler<RemoveImageCommand, Result<ProductVersionResponse>>
{
    public async Task<Result<ProductVersionResponse>> Handle(RemoveImageCommand command, CancellationToken cancellationToken)
    {
        var id = new ProductId(command.ProductId);
        var product = await db.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        // ponytail: the stored object stays behind; sweep unreferenced keys when storage cost matters.
        var removed = product.RemoveImage($"{Product.ImageKeyPrefix(id)}{command.FileName}");
        if (removed.IsFailure)
        {
            return removed.Error;
        }

        var version = await db.SaveProductAsync(product, command.Version, cancellationToken);
        if (version.IsFailure)
        {
            return version.Error;
        }

        return new ProductVersionResponse(version.Value);
    }
}
