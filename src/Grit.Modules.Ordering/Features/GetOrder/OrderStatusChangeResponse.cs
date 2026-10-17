namespace Grit.Modules.Ordering.Features.GetOrder;

internal sealed record OrderStatusChangeResponse(string Status, DateTimeOffset OccurredAt);
