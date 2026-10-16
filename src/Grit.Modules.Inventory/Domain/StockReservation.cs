namespace Grit.Modules.Inventory.Domain;

internal sealed class StockReservation
{
    private StockReservation(Guid orderId, int quantity, DateTimeOffset expiresAt)
    {
        OrderId = orderId;
        Quantity = quantity;
        ExpiresAt = expiresAt;
    }

    public Guid OrderId { get; }

    public int Quantity { get; }

    public DateTimeOffset ExpiresAt { get; }

    public bool IsCommitted { get; private set; }

    public static StockReservation Create(Guid orderId, int quantity, DateTimeOffset expiresAt) => new(orderId, quantity, expiresAt);

    public bool IsHeld(DateTimeOffset now) => IsCommitted || ExpiresAt > now;

    public void Commit() => IsCommitted = true;
}
