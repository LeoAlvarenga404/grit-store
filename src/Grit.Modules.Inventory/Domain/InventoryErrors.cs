using Grit.SharedKernel;

namespace Grit.Modules.Inventory.Domain;

internal static class InventoryErrors
{
    public static readonly Error InsufficientStock = new("inventory.insufficient_stock", "Not enough units of the SKU are available.", ErrorType.Conflict);
}
