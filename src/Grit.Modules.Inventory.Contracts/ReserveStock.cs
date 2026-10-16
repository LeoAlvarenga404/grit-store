namespace Grit.Modules.Inventory.Contracts;

public sealed record ReserveStock(Guid OrderId, IReadOnlyList<StockLine> Lines);
