namespace Grit.Modules.Catalog.Features.AddSku;

internal sealed record AddSkuRequest(
    decimal PriceAmount,
    string PriceCurrency,
    uint Version,
    Dictionary<string, string>? Attributes = null);
