namespace Grit.Modules.Ordering.Features.PlaceOrder;

internal sealed record CheckoutLine(
    Guid SkuId,
    string ProductName,
    string ProductSlug,
    string? Color,
    string? Size,
    int Quantity,
    decimal UnitPriceAmount,
    string Currency);
