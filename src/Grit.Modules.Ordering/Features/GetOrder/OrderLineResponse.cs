namespace Grit.Modules.Ordering.Features.GetOrder;

internal sealed record OrderLineResponse(Guid SkuId, string ProductName, int Quantity, decimal UnitPriceAmount, decimal LineTotalAmount);
