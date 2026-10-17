using System.Net;

namespace Grit.IntegrationTests;

public class CheckoutTests(ApiFactory factory)
{
    private readonly HttpClient _admin = AdminClient.Create(factory);
    private readonly HttpClient _customer = CustomerClient.Create(factory, Guid.NewGuid());

    [Fact]
    public async Task PlaceOrder_WithoutToken_Returns401()
    {
        var response = await CheckoutApi.PlaceOrderAsync(factory.CreateClient(), "key", "tok_visa", 0m);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PlaceOrder_WithoutIdempotencyKey_Returns400()
    {
        await BasketWithTwoUnitsAsync();

        var response = await CheckoutApi.PlaceOrderAsync(_customer, null, "tok_visa", 199.80m);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await CheckoutApi.CodeOf(response)).ShouldBe("validation.failed");
    }

    [Fact]
    public async Task PlaceOrder_EmptyBasket_Returns400()
    {
        var response = await CheckoutApi.PlaceOrderAsync(_customer, Guid.NewGuid().ToString(), "tok_visa", 0m);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await CheckoutApi.CodeOf(response)).ShouldBe("ordering.basket_empty");
    }

    [Fact]
    public async Task PlaceOrder_ExpectedSubtotalDiffers_Returns409()
    {
        await BasketWithTwoUnitsAsync();

        var response = await CheckoutApi.PlaceOrderAsync(_customer, Guid.NewGuid().ToString(), "tok_visa", 199.79m);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CheckoutApi.CodeOf(response)).ShouldBe("ordering.price_changed");
    }

    [Fact]
    public async Task PlaceOrder_ValidBasket_CreatesAnOrderWithSnapshotPricesAndFlatShipping()
    {
        var skuId = await BasketWithTwoUnitsAsync();

        var response = await CheckoutApi.PlaceOrderAsync(_customer, Guid.NewGuid().ToString(), "tok_visa", 199.80m);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await CatalogAdminApi.ReadAsync(response);
        var orderId = created.GetProperty("orderId").GetGuid();
        response.Headers.Location!.ToString().ShouldBe($"/api/v1/orders/{orderId}");
        created.GetProperty("orderNumber").GetString()!.ShouldMatch(@"^\d{4}-\d{6}$");
        var order = await CheckoutApi.GetOrderAsync(_customer, orderId);
        order.GetProperty("number").GetString().ShouldBe(created.GetProperty("orderNumber").GetString());
        var line = order.GetProperty("lines").EnumerateArray().ShouldHaveSingleItem();
        line.GetProperty("skuId").GetGuid().ShouldBe(skuId);
        line.GetProperty("quantity").GetInt32().ShouldBe(2);
        line.GetProperty("unitPriceAmount").GetDecimal().ShouldBe(99.90m);
        line.GetProperty("lineTotalAmount").GetDecimal().ShouldBe(199.80m);
        order.GetProperty("subtotalAmount").GetDecimal().ShouldBe(199.80m);
        order.GetProperty("shippingAmount").GetDecimal().ShouldBe(19.90m);
        order.GetProperty("totalAmount").GetDecimal().ShouldBe(219.70m);
        order.GetProperty("currency").GetString().ShouldBe("BRL");
        order.GetProperty("history")[0].GetProperty("status").GetString().ShouldBe("Placed");
    }

    [Fact]
    public async Task PlaceOrder_SameKeyTwice_ReturnsTheSameOrder()
    {
        await BasketWithTwoUnitsAsync();
        var key = Guid.NewGuid().ToString();

        var first = await CheckoutApi.PlaceOrderAsync(_customer, key, "tok_visa", 199.80m);
        var second = await CheckoutApi.PlaceOrderAsync(_customer, key, "tok_visa", 199.80m);

        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        second.StatusCode.ShouldBe(HttpStatusCode.Created);
        var firstId = (await CatalogAdminApi.ReadAsync(first)).GetProperty("orderId").GetGuid();
        (await CatalogAdminApi.ReadAsync(second)).GetProperty("orderId").GetGuid().ShouldBe(firstId);
        (await CheckoutApi.ListOrdersAsync(_customer)).GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task PlaceOrder_SameKeyConcurrently_CreatesOneOrder()
    {
        await BasketWithTwoUnitsAsync();
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(
            CheckoutApi.PlaceOrderAsync(_customer, key, "tok_visa", 199.80m),
            CheckoutApi.PlaceOrderAsync(_customer, key, "tok_visa", 199.80m));

        responses.ShouldContain(response => response.StatusCode == HttpStatusCode.Created);
        foreach (var response in responses.Where(response => response.StatusCode != HttpStatusCode.Created))
        {
            response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await CheckoutApi.CodeOf(response)).ShouldBe("ordering.checkout_in_progress");
        }

        (await CheckoutApi.ListOrdersAsync(_customer)).GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task GetOrder_AnotherCustomersOrder_Returns404()
    {
        await BasketWithTwoUnitsAsync();
        var orderId = await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_visa", 199.80m);
        var stranger = CustomerClient.Create(factory, Guid.NewGuid());

        var response = await stranger.GetAsync($"{CheckoutApi.OrdersPath}/{orderId}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CheckoutApi.CodeOf(response)).ShouldBe("ordering.order_not_found");
    }

    [Fact]
    public async Task CancelOrder_NotConfirmedOrder_Returns409()
    {
        await BasketWithTwoUnitsAsync();
        var orderId = await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_visa", 199.80m);

        var response = await _customer.PostAsync($"{CheckoutApi.OrdersPath}/{orderId}/cancel", null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CheckoutApi.CodeOf(response)).ShouldBe("ordering.order_not_cancellable");
    }

    [Fact]
    public async Task CancelOrder_AnotherCustomersOrder_Returns404()
    {
        var skuId = await BasketWithTwoUnitsAsync();
        await InventoryStock.SetAsync(factory, skuId, onHand: 3);
        var orderId = await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_visa", 199.80m);
        await Eventually.UntilAsync(() => CheckoutApi.GetOrderAsync(_customer, orderId), order => order.GetProperty("status").GetString() == "Confirmed");
        var stranger = CustomerClient.Create(factory, Guid.NewGuid());

        var response = await stranger.PostAsync($"{CheckoutApi.OrdersPath}/{orderId}/cancel", null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CheckoutApi.CodeOf(response)).ShouldBe("ordering.order_not_found");
        (await CheckoutApi.GetOrderAsync(_customer, orderId)).GetProperty("status").GetString().ShouldBe("Confirmed");
    }

    [Fact]
    public async Task ListOrders_OtherCustomer_DoesNotSeeTheOrder()
    {
        await BasketWithTwoUnitsAsync();
        await CheckoutApi.PlaceOrderIdAsync(_customer, "tok_visa", 199.80m);
        var stranger = CustomerClient.Create(factory, Guid.NewGuid());

        (await CheckoutApi.ListOrdersAsync(_customer)).GetArrayLength().ShouldBe(1);
        (await CheckoutApi.ListOrdersAsync(stranger)).GetArrayLength().ShouldBe(0);
    }

    private async Task<Guid> BasketWithTwoUnitsAsync()
    {
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);
        await BasketApi.AddLineAsync(_customer, skuId, 2);
        return skuId;
    }
}
