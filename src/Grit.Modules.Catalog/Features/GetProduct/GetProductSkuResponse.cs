namespace Grit.Modules.Catalog.Features.GetProduct;

internal sealed record GetProductSkuResponse(
    Guid Id,
    IReadOnlyDictionary<string, string> Attributes,
    decimal PriceAmount,
    string PriceCurrency);
