namespace Grit.Modules.Catalog.Features.SearchProducts;

internal sealed record ProductSummaryResponse(
    Guid Id,
    string Slug,
    string Name,
    decimal PriceAmount,
    string PriceCurrency,
    bool HasPriceRange,
    string? ImageUrl,
    IReadOnlyList<ProductColorResponse> Colors);
