namespace Grit.Modules.Ordering.Features.PlaceOrder;

internal sealed record PlaceOrderRequest(string PaymentToken, decimal ExpectedSubtotalAmount);
