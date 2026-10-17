using Grit.Modules.Basket.Domain;
using Grit.SharedKernel;

namespace Grit.Modules.Basket.Infrastructure;

// Money stays out of the stored JSON (ADR 0001): amount and currency are plain fields.
internal sealed record StoredBasketLine(Guid SkuId, int Quantity, decimal UnitPriceAmount, string UnitPriceCurrency, string ProductName, string ProductSlug)
{
    public static StoredBasketLine From(BasketLine line) =>
        new(line.SkuId, line.Quantity, line.UnitPrice.Amount, line.UnitPrice.Currency, line.ProductName, line.ProductSlug);

    public BasketLine ToDomain() =>
        new(SkuId, Quantity, Money.Create(UnitPriceAmount, UnitPriceCurrency).Value, ProductName, ProductSlug);
}
