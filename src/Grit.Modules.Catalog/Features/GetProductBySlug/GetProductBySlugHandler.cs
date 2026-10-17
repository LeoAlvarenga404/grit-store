using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.GetProductBySlug;

internal sealed class GetProductBySlugHandler(CatalogDbContext db, ProductImageStorage storage, CatalogCache cache)
    : IRequestHandler<GetProductBySlugQuery, Result<GetProductBySlugResponse>>
{
    public async Task<Result<GetProductBySlugResponse>> Handle(GetProductBySlugQuery query, CancellationToken cancellationToken)
    {
        // Raw route text never reaches Slug.FromName: string.Normalize throws on U+FFFE, which a URL can carry.
        if (!HasSlugShape(query.Slug))
        {
            return CatalogErrors.ProductNotFound;
        }

        // Only the canonical slug is an address; otherwise "tenis--x" would also serve the page of "tenis-x".
        var slug = Slug.FromName(query.Slug);
        if (slug.IsFailure || slug.Value.Value != query.Slug)
        {
            return CatalogErrors.ProductNotFound;
        }

        // A missing product is cached too (as null); the write that adds its first SKU clears it with everything else.
        var row = await cache.GetOrCreateAsync($"catalog:product-v2:{query.Slug}", token => FindAsync(slug.Value, token), cancellationToken);
        if (row is null)
        {
            return CatalogErrors.ProductNotFound;
        }

        // The cache holds keys, never URLs: each response signs its own, so no entry outlives a URL's hour.
        return row.Product with { ImageUrls = await storage.DownloadUrlsAsync([.. row.Images.Select(image => image.Key)]) };
    }

    // ponytail: a miss reads with this request's DbContext; if this request aborts while another waits on the same
    // key, the waiter fails too. Load with a context of its own if that ever shows up.
    private async ValueTask<GetProductBySlugRow?> FindAsync(Slug slug, CancellationToken cancellationToken) =>
        await db.Products
            .AsNoTracking()
            .Where(product => product.Slug == slug && product.Skus.Any())
            .Join(db.Categories, product => product.CategoryId, category => category.Id, (product, category) => new GetProductBySlugRow(
                new GetProductBySlugResponse(
                    product.Id.Value,
                    product.Slug.Value,
                    product.Name,
                    product.Description,
                    product.Attributes,
                    category.Slug.Value,
                    category.Name,
                    product.Skus
                        .OrderBy(sku => sku.Id)
                        .Select(sku => new GetProductBySlugSkuResponse(sku.Id.Value, sku.Attributes, sku.Price.Amount, sku.Price.Currency))
                        .ToList(),
                    Array.Empty<string>()),
                product.Images))
            .SingleOrDefaultAsync(cancellationToken);

    private static bool HasSlugShape(string text) =>
        text.Length is > 0 and <= Slug.MaxLength
        && text.All(character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-');
}
