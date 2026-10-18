namespace Grit.Modules.Catalog.Contracts;

public sealed record SkuSnapshot(
    Guid SkuId,
    string ProductName,
    string ProductSlug,
    decimal PriceAmount,
    string PriceCurrency,
    string? Color,
    string? Size,
    string? ImageUrl);
