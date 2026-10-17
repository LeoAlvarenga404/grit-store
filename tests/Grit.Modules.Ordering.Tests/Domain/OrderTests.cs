using Grit.Modules.Ordering.Domain;
using Grit.SharedKernel;

namespace Grit.Modules.Ordering.Tests.Domain;

public class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddMinutes(1);

    [Fact]
    public void Place_TwoLines_ComputesSubtotalShippingAndTotal()
    {
        var order = Place([Line(99.90m, 2), Line(10.00m, 1)], 209.80m).Value;

        order.Subtotal.Amount.ShouldBe(209.80m);
        order.ShippingCost.Amount.ShouldBe(19.90m);
        order.Total.Amount.ShouldBe(229.70m);
        order.Total.Currency.ShouldBe("BRL");
        order.Status.ShouldBe(OrderStatus.Placed);
        order.PlacedAt.ShouldBe(Now);
    }

    [Fact]
    public void Place_NoLines_ReturnsBasketEmpty()
    {
        Place([], 0m).Error.Code.ShouldBe("ordering.basket_empty");
    }

    [Fact]
    public void Place_ExpectedSubtotalDiffers_ReturnsPriceChanged()
    {
        Place([Line(99.90m, 2), Line(10.00m, 1)], 209.79m).Error.Code.ShouldBe("ordering.price_changed");
    }

    [Fact]
    public void Place_Succeeds_RecordsPlacedInTheHistory()
    {
        var order = Placed();

        order.History.Select(change => (change.Status, change.OccurredAt)).ShouldBe([(OrderStatus.Placed, Now)]);
    }

    [Fact]
    public void Confirm_PlacedOrder_ConfirmsAndAppendsToTheHistory()
    {
        var order = Placed();

        order.Confirm(Later).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Confirmed);
        order.History.Select(change => (change.Status, change.OccurredAt)).ShouldBe([(OrderStatus.Placed, Now), (OrderStatus.Confirmed, Later)]);
    }

    [Fact]
    public void Confirm_RejectedOrder_ReturnsInvalidStatus()
    {
        var order = Placed();
        order.Reject(OrderCancellationReasons.PaymentDeclined, Later);

        order.Confirm(Later).Error.Code.ShouldBe("ordering.invalid_status");
        order.Status.ShouldBe(OrderStatus.Cancelled);
    }

    [Fact]
    public void Reject_PlacedOrder_CancelsWithTheReason()
    {
        var order = Placed();

        order.Reject(OrderCancellationReasons.OutOfStock, Later).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.CancellationReason.ShouldBe("out_of_stock");
        order.History.Count.ShouldBe(2);
    }

    [Fact]
    public void Reject_ConfirmedOrder_ReturnsInvalidStatus()
    {
        var order = Placed();
        order.Confirm(Later);

        order.Reject(OrderCancellationReasons.PaymentDeclined, Later).Error.Code.ShouldBe("ordering.invalid_status");
        order.Status.ShouldBe(OrderStatus.Confirmed);
    }

    [Fact]
    public void Cancel_ConfirmedOrder_CancelsWithTheCustomerReason()
    {
        var order = Placed();
        order.Confirm(Later);

        order.Cancel(Later).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.CancellationReason.ShouldBe("customer_request");
        order.History.Select(change => change.Status).ShouldBe([OrderStatus.Placed, OrderStatus.Confirmed, OrderStatus.Cancelled]);
    }

    [Fact]
    public void Cancel_PlacedOrder_ReturnsNotCancellable()
    {
        var order = Placed();

        order.Cancel(Later).Error.Code.ShouldBe("ordering.order_not_cancellable");
        order.Status.ShouldBe(OrderStatus.Placed);
    }

    [Fact]
    public void Cancel_CancelledOrder_ReturnsNotCancellable()
    {
        var order = Placed();
        order.Confirm(Later);
        order.Cancel(Later);

        order.Cancel(Later).Error.Code.ShouldBe("ordering.order_not_cancellable");
    }

    [Fact]
    public void Ship_ConfirmedOrder_ShipsAndRecordsTheHistory()
    {
        var order = Placed();
        order.Confirm(Later);

        order.Ship(Later).IsSuccess.ShouldBeTrue();

        order.Status.ShouldBe(OrderStatus.Shipped);
        order.History.Select(change => change.Status).ShouldBe([OrderStatus.Placed, OrderStatus.Confirmed, OrderStatus.Shipped]);
    }

    [Fact]
    public void Ship_PlacedOrder_ReturnsNotShippable()
    {
        var order = Placed();

        order.Ship(Later).Error.Code.ShouldBe("ordering.order_not_shippable");
        order.Status.ShouldBe(OrderStatus.Placed);
    }

    [Fact]
    public void Ship_CancelledOrder_ReturnsNotShippable()
    {
        var order = Placed();
        order.Confirm(Later);
        order.Cancel(Later);

        order.Ship(Later).Error.Code.ShouldBe("ordering.order_not_shippable");
    }

    [Fact]
    public void Cancel_ShippedOrder_ReturnsNotCancellable()
    {
        var order = Placed();
        order.Confirm(Later);
        order.Ship(Later);

        order.Cancel(Later).Error.Code.ShouldBe("ordering.order_not_cancellable");
        order.Status.ShouldBe(OrderStatus.Shipped);
    }

    [Fact]
    public void FormatNumber_SmallSequence_PadsToSixDigits()
    {
        Order.FormatNumber(2026, 123).ShouldBe("2026-000123");
    }

    private static Order Placed() => Place([Line(99.90m, 1)], 99.90m).Value;

    private static Result<Order> Place(IReadOnlyList<OrderLine> lines, decimal expectedSubtotalAmount) =>
        Order.Place(Guid.NewGuid(), "2026-000001", "key-1", lines, expectedSubtotalAmount, Now);

    private static OrderLine Line(decimal price, int quantity) =>
        OrderLine.Create(Guid.NewGuid(), "Camiseta Dry Fit", quantity, Money.Create(price, "BRL").Value);
}
