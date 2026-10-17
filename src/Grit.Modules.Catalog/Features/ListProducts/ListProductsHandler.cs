using Grit.Modules.Catalog.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Features.ListProducts;

internal sealed class ListProductsHandler(CatalogDbContext db) : IRequestHandler<ListProductsQuery, Result<ListProductsResponse>>
{
    public const int PageSize = 50;

    // ponytail: offset paging by name is fine for an admin list of hundreds; add a name filter and keyset with the volume seed (5b).
    public async Task<Result<ListProductsResponse>> Handle(ListProductsQuery query, CancellationToken cancellationToken)
    {
        var rows = await db.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .Skip((query.Page - 1) * PageSize)
            .Take(PageSize + 1)
            .Select(product => new ProductListItemResponse(product.Id.Value, product.Name, product.Slug.Value, product.Skus.Count))
            .ToListAsync(cancellationToken);
        return new ListProductsResponse([.. rows.Take(PageSize)], rows.Count > PageSize);
    }
}
