using Grit.Modules.Ordering.Domain;

namespace Grit.Modules.Ordering.Features.GetOrder;

internal sealed record OrderResponse(
    Guid Id,
    string Number,
    string Status,
    string? CancellationReason,
    IReadOnlyList<OrderLineResponse> Lines,
    decimal SubtotalAmount,
    decimal ShippingAmount,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset PlacedAt,
    IReadOnlyList<OrderStatusChangeResponse> History)
{
    public static OrderResponse From(Order order) =>
        new(
            order.Id.Value,
            order.Number,
            order.Status.ToString(),
            order.CancellationReason,
            [.. order.Lines.Select(line => new OrderLineResponse(line.SkuId, line.ProductName, line.Quantity, line.UnitPrice.Amount, line.LineTotal.Amount))],
            order.Subtotal.Amount,
            order.ShippingCost.Amount,
            order.Total.Amount,
            order.Total.Currency,
            order.PlacedAt,
            [.. order.History.OrderBy(change => change.OccurredAt).Select(change => new OrderStatusChangeResponse(change.Status.ToString(), change.OccurredAt))]);
}
