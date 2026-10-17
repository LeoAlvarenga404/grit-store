namespace Grit.Modules.Catalog.Features.GetProductBySlug;

internal sealed record GetProductBySlugSkuResponse(
    Guid Id,
    IReadOnlyDictionary<string, string> Attributes,
    decimal PriceAmount,
    string PriceCurrency);
