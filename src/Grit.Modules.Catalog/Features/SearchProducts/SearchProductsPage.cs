namespace Grit.Modules.Catalog.Features.SearchProducts;

// What the cache holds: image keys, never signed URLs, like GetProductBySlugRow.
internal sealed record SearchProductsPage(SearchProductsResponse Response, IReadOnlyList<SearchItemImageKeys> ImageKeys);
