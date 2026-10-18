namespace Grit.Modules.Basket.Features.GetBasket;

internal sealed record BasketLineResponse(
    Guid SkuId,
    string ProductName,
    string ProductSlug,
    int Quantity,
    decimal UnitPriceAmount,
    decimal? CurrentUnitPriceAmount,
    bool HasPriceChanged,
    bool IsAvailable,
    string? Color,
    string? Size,
    string? ImageUrl);
