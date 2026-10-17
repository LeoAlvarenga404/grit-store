using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.GetProduct;

internal sealed class GetProductHandler(CatalogDbContext db, ProductImageStorage storage) : IRequestHandler<GetProductQuery, Result<GetProductResponse>>
{
    public async Task<Result<GetProductResponse>> Handle(GetProductQuery query, CancellationToken cancellationToken)
    {
        var id = new ProductId(query.Id);
        var row = await db.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new
            {
                product.ImageKeys,
                Product = new GetProductResponse(
                    product.Id.Value,
                    product.CategoryId.Value,
                    product.Name,
                    product.Slug.Value,
                    product.Description,
                    product.Attributes,
                    EF.Property<uint>(product, CatalogDbContext.Version),
                    product.Skus
                        .OrderBy(sku => sku.Id)
                        .Select(sku => new GetProductSkuResponse(sku.Id.Value, sku.Attributes, sku.Price.Amount, sku.Price.Currency))
                        .ToList(),
                    Array.Empty<ProductImageResponse>()),
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        // Signed after the query: the database holds keys, the response carries URLs valid for an hour.
        var urls = await storage.DownloadUrlsAsync(row.ImageKeys);
        return row.Product with
        {
            Images = [.. row.ImageKeys.Select((key, index) => new ProductImageResponse(key, key[(key.LastIndexOf('/') + 1)..], urls[index]))],
        };
    }
}
