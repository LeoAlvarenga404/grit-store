using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.ChangeSkuPrice;

internal sealed class ChangeSkuPriceHandler(CatalogDbContext db)
    : IRequestHandler<ChangeSkuPriceCommand, Result<ProductVersionResponse>>
{
    public async Task<Result<ProductVersionResponse>> Handle(ChangeSkuPriceCommand command, CancellationToken cancellationToken)
    {
        var price = Money.Create(command.PriceAmount, command.PriceCurrency);
        if (price.IsFailure)
        {
            return price.Error;
        }

        var id = new ProductId(command.ProductId);
        var product = await db.Products.Include(product => product.Skus).SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
        if (product is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        var changed = product.ChangeSkuPrice(new SkuId(command.SkuId), price.Value);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var version = await db.SaveProductAsync(product, command.Version, cancellationToken);
        if (version.IsFailure)
        {
            return version.Error;
        }

        return new ProductVersionResponse(version.Value);
    }
}
