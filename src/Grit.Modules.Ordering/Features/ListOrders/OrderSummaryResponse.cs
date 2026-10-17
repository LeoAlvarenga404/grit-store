namespace Grit.Modules.Ordering.Features.ListOrders;

internal sealed record OrderSummaryResponse(Guid Id, string Number, string Status, decimal TotalAmount, string Currency, DateTimeOffset PlacedAt);
