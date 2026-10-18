namespace Grit.Modules.Ordering.Features.GetOrder;

internal sealed record OrderLineResponse(
    Guid SkuId,
    string ProductName,
    string? ProductSlug,
    string? Color,
    string? Size,
    string? ImageUrl,
    int Quantity,
    decimal UnitPriceAmount,
    decimal LineTotalAmount);
