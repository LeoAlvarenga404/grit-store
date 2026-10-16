using Grit.SharedKernel;

namespace Grit.Modules.Basket.Domain;

internal static class BasketErrors
{
    public static readonly Error QuantityLimit = new("basket.quantity_limit", "The quantity of an item must be between 1 and 10.", ErrorType.Validation);

    public static readonly Error LineLimit = new("basket.line_limit", "The basket holds at most 50 different items.", ErrorType.Validation);

    public static readonly Error LineNotFound = new("basket.line_not_found", "The basket has no item with this SKU.", ErrorType.NotFound);

    public static readonly Error SkuNotFound = new("basket.sku_not_found", "The SKU does not exist.", ErrorType.NotFound);
}
