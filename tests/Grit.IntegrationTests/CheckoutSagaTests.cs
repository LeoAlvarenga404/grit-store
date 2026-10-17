using System.Net;
using System.Text.Json;

namespace Grit.IntegrationTests;

// Showcase (design §13): the whole checkout across Basket, Ordering, Inventory and Payments, through the outboxes.
public class CheckoutSagaTests(ApiFactory factory)
{
    private readonly HttpClient _admin = factory.CreateClient();
    private readonly HttpClient _customer = CustomerClient.Create(factory, Guid.NewGuid());

    [Fact]
    public async Task Checkout_PaymentAuthorized_ConfirmsTheOrderAndCommitsTheReservation()
    {
        var skuId = await StockedBasketAsync(onHand: 3, quantity: 2);

        var orderId = await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_visa", 199.80m);

        var order = await WaitForStatusAsync(orderId, "Confirmed");
        order.GetProperty("status").GetString().ShouldBe("Confirmed");
        Statuses(order).ShouldBe(["Placed", "Confirmed"]);
        var stock = await Eventually.UntilAsync(() => InventoryStock.GetAsync(factory, skuId), item => item.Reservations.All(reservation => reservation.IsCommitted));
        var reservation = stock.Reservations.ShouldHaveSingleItem();
        reservation.OrderId.ShouldBe(orderId);
        reservation.Quantity.ShouldBe(2);
        reservation.IsCommitted.ShouldBeTrue();
        stock.Available(DateTimeOffset.UtcNow.AddHours(1)).ShouldBe(1);
        var basket = await Eventually.UntilAsync(() => BasketApi.GetAsync(_customer), value => value.GetProperty("lines").GetArrayLength() == 0);
        basket.GetProperty("lines").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Checkout_PaymentDeclined_CancelsTheOrderAndReleasesTheReservation()
    {
        var skuId = await StockedBasketAsync(onHand: 3, quantity: 2);

        var orderId = await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_decline", 199.80m);

        var order = await WaitForStatusAsync(orderId, "Cancelled");
        order.GetProperty("status").GetString().ShouldBe("Cancelled");
        order.GetProperty("cancellationReason").GetString().ShouldBe("payment_declined");
        Statuses(order).ShouldBe(["Placed", "Cancelled"]);
        var stock = await Eventually.UntilAsync(() => InventoryStock.GetAsync(factory, skuId), item => item.Reservations.Count == 0);
        stock.Reservations.ShouldBeEmpty();
        stock.Available(DateTimeOffset.UtcNow).ShouldBe(3);
    }

    [Fact]
    public async Task Checkout_NotEnoughStock_CancelsTheOrderAsOutOfStock()
    {
        var skuId = await StockedBasketAsync(onHand: 1, quantity: 2);

        var orderId = await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_visa", 199.80m);

        var order = await WaitForStatusAsync(orderId, "Cancelled");
        order.GetProperty("cancellationReason").GetString().ShouldBe("out_of_stock");
        (await InventoryStock.GetAsync(factory, skuId)).Reservations.ShouldBeEmpty();
    }

    [Fact]
    public async Task CancelOrder_ConfirmedOrder_CancelsAndReleasesTheStock()
    {
        var skuId = await StockedBasketAsync(onHand: 3, quantity: 2);
        var orderId = await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_visa", 199.80m);
        await WaitForStatusAsync(orderId, "Confirmed");

        var response = await _customer.PostAsync($"{CheckoutApi.OrdersPath}/{orderId}/cancel", null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var order = await CheckoutApi.GetOrderAsync(_customer, orderId);
        order.GetProperty("status").GetString().ShouldBe("Cancelled");
        order.GetProperty("cancellationReason").GetString().ShouldBe("customer_request");
        var stock = await Eventually.UntilAsync(() => InventoryStock.GetAsync(factory, skuId), item => item.Reservations.Count == 0);
        stock.Reservations.ShouldBeEmpty();
        stock.Available(DateTimeOffset.UtcNow).ShouldBe(3);
    }

    [Fact]
    public async Task CancelOrder_ConfirmedOrder_RefundsThePaymentAuthorization()
    {
        await using var payments = await PaymentListener.StartAsync(factory);
        await StockedBasketAsync(onHand: 3, quantity: 2);
        var orderId = await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_visa", 199.80m);
        await WaitForStatusAsync(orderId, "Confirmed");

        var response = await _customer.PostAsync($"{CheckoutApi.OrdersPath}/{orderId}/cancel", null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var refunded = await Eventually.UntilAsync(() => Task.FromResult(payments.RefundOf(orderId)), authorizationId => authorizationId is not null);
        refunded.ShouldNotBeNull();
        refunded.ShouldStartWith("fake_");
        refunded.ShouldBe(payments.AuthorizationOf(orderId));
    }

    [Fact]
    public async Task CancelOrder_TwiceAtOnce_CancelsOnceAndRecordsOneHistoryEntry()
    {
        for (var round = 0; round < 5; round++)
        {
            await StockedBasketAsync(onHand: 3, quantity: 2);
            var orderId = await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_visa", 199.80m);
            await WaitForStatusAsync(orderId, "Confirmed");

            var responses = await Task.WhenAll(
                _customer.PostAsync($"{CheckoutApi.OrdersPath}/{orderId}/cancel", null, TestContext.Current.CancellationToken),
                _customer.PostAsync($"{CheckoutApi.OrdersPath}/{orderId}/cancel", null, TestContext.Current.CancellationToken));

            responses.Select(response => response.StatusCode).Order().ShouldBe([HttpStatusCode.NoContent, HttpStatusCode.Conflict]);
            var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
            (await CheckoutApi.CodeOf(conflict)).ShouldBe("ordering.order_not_cancellable");
            var order = await CheckoutApi.GetOrderAsync(_customer, orderId);
            Statuses(order).Count(status => status == "Cancelled").ShouldBe(1);
        }
    }

    private static string?[] Statuses(JsonElement order) =>
        [.. order.GetProperty("history").EnumerateArray().Select(change => change.GetProperty("status").GetString())];

    private Task<JsonElement> WaitForStatusAsync(Guid orderId, string status) =>
        Eventually.UntilAsync(() => CheckoutApi.GetOrderAsync(_customer, orderId), order => order.GetProperty("status").GetString() == status);

    private async Task<Guid> StockedBasketAsync(int onHand, int quantity)
    {
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);
        await InventoryStock.SetAsync(factory, skuId, onHand);
        await BasketApi.AddLineAsync(_customer, skuId, quantity);
        return skuId;
    }
}
