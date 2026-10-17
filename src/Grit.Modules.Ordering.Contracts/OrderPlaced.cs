namespace Grit.Modules.Ordering.Contracts;

public sealed record OrderPlaced(
    Guid OrderId,
    Guid CustomerId,
    IReadOnlyList<OrderPlacedLine> Lines,
    decimal TotalAmount,
    string Currency,
    string PaymentToken);
