using Grit.SharedKernel;

namespace Grit.Modules.Ordering.Domain;

internal static class OrderingErrors
{
    public static readonly Error BasketEmpty = new("ordering.basket_empty", "The basket is empty.", ErrorType.Validation);

    public static readonly Error PriceChanged = new("ordering.price_changed", "Prices changed since the basket was shown. Reload the basket and try again.", ErrorType.Conflict);

    public static readonly Error SkuUnavailable = new("ordering.sku_unavailable", "An item in the basket is no longer sold. Remove it and try again.", ErrorType.Conflict);

    public static readonly Error CheckoutInProgress = new("ordering.checkout_in_progress", "A checkout with this Idempotency-Key is still running. Retry the same request.", ErrorType.Conflict);

    public static readonly Error OrderNotFound = new("ordering.order_not_found", "The order does not exist.", ErrorType.NotFound);

    public static readonly Error InvalidStatus = new("ordering.invalid_status", "The order status does not allow this change.", ErrorType.Conflict);

    public static readonly Error OrderNotCancellable = new("ordering.order_not_cancellable", "Only confirmed orders can be cancelled.", ErrorType.Conflict);
}
