namespace Grit.Modules.Ordering.Features.PlaceOrder;

internal sealed record CheckoutLine(Guid SkuId, string ProductName, int Quantity, decimal UnitPriceAmount, string Currency);
