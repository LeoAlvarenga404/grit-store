namespace Grit.Modules.Ordering.Features.ListAllOrders;

internal sealed record OrderOverviewResponse(Guid Id, string Number, Guid CustomerId, string Status, decimal TotalAmount, string Currency, DateTimeOffset PlacedAt);
