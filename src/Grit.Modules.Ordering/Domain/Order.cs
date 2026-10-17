using Grit.SharedKernel;

namespace Grit.Modules.Ordering.Domain;

internal sealed class Order : AggregateRoot<OrderId>
{
    // Flat rate until a shipping table exists (design §8 asks for one; this slice keeps a constant).
    public static readonly Money FlatShipping = Money.Create(19.90m, "BRL").Value;

    private readonly List<OrderLine> _lines = [];
    private readonly List<OrderStatusChange> _history = [];

    // EF cannot bind complex properties (the Money totals) to constructor parameters; Place sets them right after.
    private Order(OrderId id, Guid customerId, string number, string idempotencyKey) : base(id)
    {
        CustomerId = customerId;
        Number = number;
        IdempotencyKey = idempotencyKey;
        Subtotal = null!;
        ShippingCost = null!;
        Total = null!;
    }

    public Guid CustomerId { get; }

    public string Number { get; }

    public string IdempotencyKey { get; }

    public OrderStatus Status { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTimeOffset PlacedAt { get; private set; }

    public Money Subtotal { get; private set; }

    public Money ShippingCost { get; private set; }

    public Money Total { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public IReadOnlyList<OrderStatusChange> History => _history;

    public static string FormatNumber(int year, long sequence) => $"{year}-{sequence:D6}";

    public static Result<Order> Place(
        Guid customerId,
        string number,
        string idempotencyKey,
        IReadOnlyList<OrderLine> lines,
        decimal expectedSubtotalAmount,
        DateTimeOffset now)
    {
        if (lines.Count == 0)
        {
            return OrderingErrors.BasketEmpty;
        }

        var subtotal = lines.Skip(1).Aggregate(lines[0].LineTotal, (sum, line) => sum.Add(line.LineTotal));
        if (subtotal.Amount != expectedSubtotalAmount)
        {
            return OrderingErrors.PriceChanged;
        }

        var order = new Order(OrderId.New(), customerId, number, idempotencyKey)
        {
            Subtotal = subtotal,
            ShippingCost = FlatShipping,
            Total = subtotal.Add(FlatShipping),
            PlacedAt = now,
        };
        order._lines.AddRange(lines);
        order.ChangeStatus(OrderStatus.Placed, now);
        return order;
    }

    public Result Confirm(DateTimeOffset now)
    {
        if (Status != OrderStatus.Placed)
        {
            return OrderingErrors.InvalidStatus;
        }

        ChangeStatus(OrderStatus.Confirmed, now);
        return Result.Success();
    }

    public Result Reject(string reason, DateTimeOffset now)
    {
        if (Status != OrderStatus.Placed)
        {
            return OrderingErrors.InvalidStatus;
        }

        CancellationReason = reason;
        ChangeStatus(OrderStatus.Cancelled, now);
        return Result.Success();
    }

    // Only a confirmed order: while the saga runs, a customer cancel would race it, and a shipped order has left the store (design §8).
    public Result Cancel(DateTimeOffset now)
    {
        if (Status != OrderStatus.Confirmed)
        {
            return OrderingErrors.OrderNotCancellable;
        }

        CancellationReason = OrderCancellationReasons.CustomerRequest;
        ChangeStatus(OrderStatus.Cancelled, now);
        return Result.Success();
    }

    public Result Ship(DateTimeOffset now)
    {
        if (Status != OrderStatus.Confirmed)
        {
            return OrderingErrors.OrderNotShippable;
        }

        ChangeStatus(OrderStatus.Shipped, now);
        return Result.Success();
    }

    private void ChangeStatus(OrderStatus status, DateTimeOffset now)
    {
        Status = status;
        _history.Add(new OrderStatusChange(status, now));
    }
}
