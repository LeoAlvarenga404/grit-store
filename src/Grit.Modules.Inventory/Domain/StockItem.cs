using Grit.SharedKernel;

namespace Grit.Modules.Inventory.Domain;

// Keyed by the catalog SKU id. On-hand units only drop when an order ships (admin, 5a); until then a committed reservation holds them.
internal sealed class StockItem : AggregateRoot<Guid>
{
    public static readonly TimeSpan ReservationLifetime = TimeSpan.FromMinutes(15);

    private readonly List<StockReservation> _reservations = [];

    private StockItem(Guid id, int onHand) : base(id)
    {
        OnHand = onHand;
    }

    public int OnHand { get; private set; }

    public IReadOnlyList<StockReservation> Reservations => _reservations;

    public static StockItem Create(Guid skuId, int onHand)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(onHand);

        return new StockItem(skuId, onHand);
    }

    public int Available(DateTimeOffset now) => OnHand - _reservations.Where(reservation => reservation.IsHeld(now)).Sum(reservation => reservation.Quantity);

    public bool CanReserve(Guid orderId, int quantity, DateTimeOffset now) => HasReservation(orderId) || Available(now) >= quantity;

    // ponytail: an expired reservation is skipped, not deleted; rows of abandoned checkouts stay until released (cleanup job if they pile up).
    public Result Reserve(Guid orderId, int quantity, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        if (HasReservation(orderId))
        {
            return Result.Success();
        }

        if (Available(now) < quantity)
        {
            return InventoryErrors.InsufficientStock;
        }

        _reservations.Add(StockReservation.Create(orderId, quantity, now + ReservationLifetime));
        return Result.Success();
    }

    // ponytail: commit ignores expiry, so a payment later than the lifetime can oversell; add a failed-commit reply with a slow gateway.
    public void Commit(Guid orderId) => _reservations.Find(reservation => reservation.OrderId == orderId)?.Commit();

    public void Release(Guid orderId) => _reservations.RemoveAll(reservation => reservation.OrderId == orderId);

    private bool HasReservation(Guid orderId) => _reservations.Exists(reservation => reservation.OrderId == orderId);
}
