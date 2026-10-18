namespace Grit.Modules.Catalog.Infrastructure;

// Column order matches the SQL: Dapper binds record constructors by position and name.
internal sealed record SkuSnapshotRow(
    Guid SkuId,
    string ProductName,
    string ProductSlug,
    decimal PriceAmount,
    string PriceCurrency,
    string? Color,
    string? Size,
    string? ImageKey);
