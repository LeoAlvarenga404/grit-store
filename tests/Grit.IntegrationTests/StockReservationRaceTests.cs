using Grit.Modules.Inventory.Contracts;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class StockReservationRaceTests(ApiFactory factory)
{
    [Fact]
    public async Task ReserveStock_TwoBuyersForTheLastUnit_ReservesForExactlyOne()
    {
        var skuId = Guid.NewGuid();
        await InventoryStock.SetAsync(factory, skuId, onHand: 1);
        Guid[] orderIds = [Guid.NewGuid(), Guid.NewGuid()];
        await using var listener = await StockOutcomeListener.StartAsync(factory, orderIds);
        var bus = factory.Services.GetRequiredService<IBus>();

        await Task.WhenAll(orderIds.Select(orderId =>
            bus.Publish(new ReserveStock(orderId, [new StockLine(skuId, 1)]), TestContext.Current.CancellationToken)));

        await listener.Completed.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        listener.Outcomes.Values.Count(isReserved => isReserved).ShouldBe(1);
        (await InventoryStock.GetAsync(factory, skuId)).Reservations.ShouldHaveSingleItem().Quantity.ShouldBe(1);
    }
}
