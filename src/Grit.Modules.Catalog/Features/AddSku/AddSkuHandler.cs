using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.AddSku;

internal sealed class AddSkuHandler(CatalogDbContext db) : IRequestHandler<AddSkuCommand, Result<AddSkuResponse>>
{
    public async Task<Result<AddSkuResponse>> Handle(AddSkuCommand command, CancellationToken cancellationToken)
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

        var sku = product.AddSku(command.Attributes, price.Value);
        if (sku.IsFailure)
        {
            return sku.Error;
        }

        var version = await db.SaveProductAsync(product, command.Version, cancellationToken);
        if (version.IsFailure)
        {
            return version.Error;
        }

        return new AddSkuResponse(sku.Value.Id.Value, version.Value);
    }
}
