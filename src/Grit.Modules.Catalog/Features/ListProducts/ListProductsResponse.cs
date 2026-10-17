namespace Grit.Modules.Catalog.Features.ListProducts;

internal sealed record ListProductsResponse(IReadOnlyList<ProductListItemResponse> Items, bool HasNextPage);
