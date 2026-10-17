using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

// Each order has a customer of its own, so no test waits for another order's basket to clear.
public class AdminOrderTests(ApiFactory factory)
{
    private const string AdminOrdersPath = "/api/v1/admin/orders";

    private readonly HttpClient _admin = AdminClient.Create(factory);

    [Fact]
    public async Task ShipOrder_ConfirmedOrder_ShipsAndDropsOnHandByTheReservation()
    {
        var (customer, orderId, skuId) = await ConfirmedOrderAsync(quantity: 2);

        var response = await ShipAsync(orderId);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var order = await CheckoutApi.GetOrderAsync(customer, orderId);
        order.GetProperty("status").GetString().ShouldBe("Shipped");
        string?[] statuses = [.. order.GetProperty("history").EnumerateArray().Select(change => change.GetProperty("status").GetString())];
        statuses.ShouldBe(["Placed", "Confirmed", "Shipped"]);
        var stock = await Eventually.UntilAsync(() => InventoryStock.GetAsync(factory, skuId), item => item.Reservations.Count == 0);
        stock.Reservations.ShouldBeEmpty();
        stock.OnHand.ShouldBe(1);
    }

    [Fact]
    public async Task CancelOrder_ShippedOrder_Returns409()
    {
        var (customer, orderId, _) = await ConfirmedOrderAsync(quantity: 1);
        (await ShipAsync(orderId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await customer.PostAsync($"{CheckoutApi.OrdersPath}/{orderId}/cancel", null, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("ordering.order_not_cancellable");
    }

    [Fact]
    public async Task ShipOrder_CancelledOrder_Returns409()
    {
        var (customer, orderId, _) = await PlaceOrderAsync("tok_decline", quantity: 1);
        await WaitForStatusAsync(customer, orderId, "Cancelled");

        var response = await ShipAsync(orderId);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("ordering.order_not_shippable");
    }

    [Fact]
    public async Task ShipOrder_UnknownOrder_Returns404()
    {
        var response = await ShipAsync(Guid.NewGuid());

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.NotFound);
        problem.GetProperty("code").GetString().ShouldBe("ordering.order_not_found");
    }

    [Fact]
    public async Task ListAllOrders_ByStatus_ReturnsOnlyThatStatusNewestFirst()
    {
        var cancelled = await PlaceOrderAsync("tok_decline", quantity: 1);
        var confirmed = await ConfirmedOrderAsync(quantity: 1);
        await WaitForStatusAsync(cancelled.Customer, cancelled.OrderId, "Cancelled");

        var list = await _admin.GetFromJsonAsync<JsonElement>($"{AdminOrdersPath}?status=Cancelled&limit=100", TestContext.Current.CancellationToken);

        var items = list.GetProperty("items").EnumerateArray().ToList();
        items.ShouldContain(item => item.GetProperty("id").GetGuid() == cancelled.OrderId);
        items.ShouldNotContain(item => item.GetProperty("id").GetGuid() == confirmed.OrderId);
        items.ShouldAllBe(item => item.GetProperty("status").GetString() == "Cancelled");
        var numbers = items.Select(item => item.GetProperty("number").GetString()!).ToList();
        numbers.ShouldBe([.. numbers.OrderDescending(StringComparer.Ordinal)]);
    }

    [Fact]
    public async Task ListAllOrders_LimitOne_NextCursorReadsTheOlderPage()
    {
        await PlaceOrderAsync("tok_visa", quantity: 1);
        await PlaceOrderAsync("tok_visa", quantity: 1);

        var first = await _admin.GetFromJsonAsync<JsonElement>($"{AdminOrdersPath}?limit=1", TestContext.Current.CancellationToken);
        var firstNumber = first.GetProperty("items").EnumerateArray().ShouldHaveSingleItem().GetProperty("number").GetString();
        var cursor = first.GetProperty("nextCursor").GetString();
        cursor.ShouldBe(firstNumber);
        var second = await _admin.GetFromJsonAsync<JsonElement>($"{AdminOrdersPath}?limit=1&after={cursor}", TestContext.Current.CancellationToken);

        var secondNumber = second.GetProperty("items").EnumerateArray().ShouldHaveSingleItem().GetProperty("number").GetString();
        string.CompareOrdinal(secondNumber, firstNumber).ShouldBeLessThan(0);
    }

    [Theory]
    [InlineData("Lost")]
    [InlineData("1")]
    public async Task ListAllOrders_UnknownStatus_Returns400(string status)
    {
        var response = await _admin.GetAsync($"{AdminOrdersPath}?status={status}", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
    }

    [Fact]
    public async Task ListAllOrders_CustomerToken_Returns403()
    {
        var customer = CustomerClient.Create(factory, Guid.NewGuid());

        var response = await customer.GetAsync(AdminOrdersPath, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<(HttpClient Customer, Guid OrderId, Guid SkuId)> PlaceOrderAsync(string paymentToken, int quantity)
    {
        var customer = CustomerClient.Create(factory, Guid.NewGuid());
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);
        await InventoryStock.SetAsync(factory, skuId, 3);
        await BasketApi.AddLineAsync(customer, skuId, quantity);
        var orderId = await CheckoutApi.PlaceOrderIdAsync(customer, paymentToken, 99.90m * quantity);
        return (customer, orderId, skuId);
    }

    private async Task<(HttpClient Customer, Guid OrderId, Guid SkuId)> ConfirmedOrderAsync(int quantity)
    {
        var placed = await PlaceOrderAsync("tok_visa", quantity);
        await WaitForStatusAsync(placed.Customer, placed.OrderId, "Confirmed");
        return placed;
    }

    private static Task<JsonElement> WaitForStatusAsync(HttpClient customer, Guid orderId, string status) =>
        Eventually.UntilAsync(() => CheckoutApi.GetOrderAsync(customer, orderId), order => order.GetProperty("status").GetString() == status);

    private Task<HttpResponseMessage> ShipAsync(Guid orderId) =>
        _admin.PostAsync($"{AdminOrdersPath}/{orderId}/ship", null, TestContext.Current.CancellationToken);
}
