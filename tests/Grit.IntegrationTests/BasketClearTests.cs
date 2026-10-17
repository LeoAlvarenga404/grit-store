using System.Text.Json;
using Grit.Modules.Basket.Infrastructure;
using Grit.Modules.Ordering.Contracts;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

// OrderPlaced reaches the basket after checkout answers, so the customer may already have added lines by then.
public class BasketClearTests(ApiFactory factory)
{
    private readonly HttpClient _admin = AdminClient.Create(factory);

    [Fact]
    public async Task OrderPlaced_DeliveredAfterNewLinesWereAdded_RemovesOnlyThePurchasedQuantities()
    {
        var customerId = Guid.NewGuid();
        var customer = CustomerClient.Create(factory, customerId);
        var (_, _, purchased) = await BasketApi.CreateSkuAsync(_admin);
        var (_, _, addedLater) = await BasketApi.CreateSkuAsync(_admin);
        await BasketApi.AddLineAsync(customer, purchased, 3);
        await BasketApi.AddLineAsync(customer, addedLater, 1);

        await SendToClearBasketAsync(PlacedOrder(customerId, new OrderPlacedLine(purchased, 2)));

        var basket = await Eventually.UntilAsync(() => BasketApi.GetAsync(customer), value => QuantityOf(value, purchased) == 1);
        QuantityOf(basket, purchased).ShouldBe(1);
        QuantityOf(basket, addedLater).ShouldBe(1);
    }

    [Fact]
    public async Task RemovePurchasedAsync_SameOrderAgainAfterTheBasketWasRefilled_KeepsTheNewLines()
    {
        var customerId = Guid.NewGuid();
        var customer = CustomerClient.Create(factory, customerId);
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);
        var store = factory.Services.GetRequiredService<BasketStore>();
        var order = PlacedOrder(customerId, new OrderPlacedLine(skuId, 2));
        await BasketApi.AddLineAsync(customer, skuId, 2);
        await store.RemovePurchasedAsync(order);
        QuantityOf(await BasketApi.GetAsync(customer), skuId).ShouldBe(0);
        await BasketApi.AddLineAsync(customer, skuId, 2);

        await store.RemovePurchasedAsync(order);

        QuantityOf(await BasketApi.GetAsync(customer), skuId).ShouldBe(2);
    }

    private static OrderPlaced PlacedOrder(Guid customerId, OrderPlacedLine line) =>
        new(Guid.NewGuid(), customerId, [line], 199.80m, "BRL", "tok_visa");

    // Sent to the basket's queue only: a published OrderPlaced would also start a checkout saga for an order that does not exist.
    private async Task SendToClearBasketAsync(OrderPlaced order)
    {
        var queue = DefaultEndpointNameFormatter.Instance.Consumer<ClearBasketConsumer>();
        var endpoint = await factory.Services.GetRequiredService<IBus>().GetSendEndpoint(new Uri($"queue:{queue}"));
        await endpoint.Send(order, TestContext.Current.CancellationToken);
    }

    private static int QuantityOf(JsonElement basket, Guid skuId) =>
        basket.GetProperty("lines").EnumerateArray()
            .Where(line => line.GetProperty("skuId").GetGuid() == skuId)
            .Select(line => line.GetProperty("quantity").GetInt32())
            .SingleOrDefault();
}
