namespace Grit.Modules.Catalog.Features.ChangeSkuPrice;

internal sealed record ChangeSkuPriceRequest(decimal PriceAmount, string PriceCurrency, uint Version);
