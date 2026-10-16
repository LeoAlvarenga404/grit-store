namespace Grit.Modules.Catalog.Features.SearchProducts;

internal sealed record SearchProductsResponse(IReadOnlyList<ProductSummaryResponse> Items, string? NextCursor);
