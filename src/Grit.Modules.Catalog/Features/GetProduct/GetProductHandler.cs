using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.GetProduct;

internal sealed class GetProductHandler(CatalogDbContext db) : IRequestHandler<GetProductQuery, Result<GetProductResponse>>
{
    public async Task<Result<GetProductResponse>> Handle(GetProductQuery query, CancellationToken cancellationToken)
    {
        var id = new ProductId(query.Id);
        var product = await db.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new GetProductResponse(
                product.Id.Value,
                product.CategoryId.Value,
                product.Name,
                product.Slug.Value,
                product.Description,
                product.Attributes,
                EF.Property<uint>(product, CatalogDbContext.Version),
                product.Skus
                    .Select(sku => new GetProductSkuResponse(sku.Id.Value, sku.Attributes, sku.Price.Amount, sku.Price.Currency))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return product is null ? CatalogErrors.ProductNotFound : product;
    }
}
