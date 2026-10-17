namespace Grit.Modules.Inventory.Contracts;

public sealed record StockReservationFailed(Guid OrderId, string Reason);
