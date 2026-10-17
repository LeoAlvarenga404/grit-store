using Grit.SharedKernel;

namespace Grit.Modules.Inventory.Domain;

internal static class InventoryErrors
{
    public static readonly Error InsufficientStock = new("inventory.insufficient_stock", "Not enough units of the SKU are available.", ErrorType.Conflict);

    public static readonly Error BelowHeldUnits = new("inventory.below_held_units", "On-hand units cannot drop below the units held by reservations.", ErrorType.Conflict);

    public static readonly Error SkuNotFound = new("inventory.sku_not_found", "The catalog has no SKU with this id.", ErrorType.NotFound);
}
