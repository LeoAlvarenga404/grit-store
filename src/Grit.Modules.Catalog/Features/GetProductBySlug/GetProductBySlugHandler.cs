using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.GetProductBySlug;

internal sealed class GetProductBySlugHandler(CatalogDbContext db) : IRequestHandler<GetProductBySlugQuery, Result<GetProductBySlugResponse>>
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

        var product = await db.Products
            .AsNoTracking()
            .Where(product => product.Slug == slug.Value && product.Skus.Any())
            .Join(db.Categories, product => product.CategoryId, category => category.Id, (product, category) => new GetProductBySlugResponse(
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
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return product is null ? CatalogErrors.ProductNotFound : product;
    }

    private static bool HasSlugShape(string text) =>
        text.Length is > 0 and <= Slug.MaxLength
        && text.All(character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-');
}
