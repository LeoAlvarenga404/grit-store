namespace Grit.Modules.Ordering.Domain;

internal sealed class OrderStatusChange(OrderStatus status, DateTimeOffset occurredAt)
{
    public OrderStatus Status { get; } = status;

    public DateTimeOffset OccurredAt { get; } = occurredAt;
}
