using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Grit.Modules.Inventory.Contracts;
using Grit.Modules.Inventory.Infrastructure;
using Grit.Modules.Ordering.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Grit.IntegrationTests;

public class AdminConcurrencyTests(ApiFactory factory)
{
    private const string AdminOrdersPath = "/api/v1/admin/orders";
    private const string StockPath = "/api/v1/admin/stock";

    private readonly HttpClient _admin = AdminClient.Create(factory);

    [Fact]
    public async Task ShipAndCancel_AtOnce_OneWinsAndTheOtherGets409()
    {
        for (var round = 0; round < 5; round++)
        {
            // Alternating which request queues first: a lock removed from either handler lets the other one through in some round.
            var shouldShipQueueFirst = round % 2 == 1;
            var (customer, orderId, skuId) = await ConfirmedOrderAsync(onHand: 3, quantity: 2);
            await using var rowLock = await RowLock.AcquireAsync<OrderingDbContext>(factory, $"SELECT 1 FROM ordering.orders WHERE id = {orderId} FOR UPDATE");

            var firstTask = shouldShipQueueFirst ? ShipAsync(orderId) : CancelAsync(customer, orderId);
            await rowLock.WaitForWaitersAsync(1);
            var secondTask = shouldShipQueueFirst ? CancelAsync(customer, orderId) : ShipAsync(orderId);
            await rowLock.WaitForWaitersAsync(2);
            await rowLock.ReleaseAsync();
            var first = await firstTask;
            var second = await secondTask;
            var ship = shouldShipQueueFirst ? first : second;
            var cancel = shouldShipQueueFirst ? second : first;

            HttpStatusCode[] statuses = [ship.StatusCode, cancel.StatusCode];
            statuses.Order().ShouldBe([HttpStatusCode.NoContent, HttpStatusCode.Conflict]);
            var isShipWinner = ship.StatusCode == HttpStatusCode.NoContent;
            (await CheckoutApi.CodeOf(isShipWinner ? cancel : ship)).ShouldBe(isShipWinner ? "ordering.order_not_cancellable" : "ordering.order_not_shippable");
            var order = await CheckoutApi.GetOrderAsync(customer, orderId);
            order.GetProperty("status").GetString().ShouldBe(isShipWinner ? "Shipped" : "Cancelled");
            Statuses(order).Count(status => status is "Shipped" or "Cancelled").ShouldBe(1);
            var stock = await Eventually.UntilAsync(() => InventoryStock.GetAsync(factory, skuId), item => item.Reservations.Count == 0);
            stock.Reservations.ShouldBeEmpty();
            stock.OnHand.ShouldBe(isShipWinner ? 1 : 3);
        }
    }

    [Fact]
    public async Task SetStock_WhileAReservationIsMade_NeverGoesBelowHeldUnits()
    {
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);
        await InventoryStock.SetAsync(factory, skuId, onHand: 5);
        var orderId = Guid.NewGuid();
        await using var listener = await StockOutcomeListener.StartAsync(factory, [orderId]);
        var bus = factory.Services.GetRequiredService<IBus>();
        await using var rowLock = await RowLock.AcquireAsync<InventoryDbContext>(factory, $"SELECT sku_id FROM inventory.stock_items WHERE sku_id = {skuId} FOR UPDATE");

        await bus.Publish(new ReserveStock(orderId, [new StockLine(skuId, 3)]), TestContext.Current.CancellationToken);
        await rowLock.WaitForWaitersAsync(1);
        var setTask = SetAsync(skuId, 2);
        await rowLock.WaitForWaitersAsync(2);
        await rowLock.ReleaseAsync();

        var response = await setTask;
        await listener.Completed.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        var stock = await InventoryStock.GetAsync(factory, skuId);
        var wasReserved = listener.Outcomes[orderId];
        // Serial orders: reserve then set (the set is refused, 5 on hand and 3 held) or set then reserve (the reserve is refused, 2 on hand).
        var isReserveThenSet = response.StatusCode == HttpStatusCode.Conflict && wasReserved && stock.OnHand == 5;
        var isSetThenReserve = response.StatusCode == HttpStatusCode.NoContent && !wasReserved && stock.OnHand == 2;
        (isReserveThenSet || isSetThenReserve).ShouldBeTrue($"set {response.StatusCode}, reserved {wasReserved}, on hand {stock.OnHand}");
        stock.OnHand.ShouldBeGreaterThanOrEqualTo(stock.Held(DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task SetStock_WhileAnOrderShips_KeepsBothChanges()
    {
        var (_, orderId, skuId) = await ConfirmedOrderAsync(onHand: 5, quantity: 3);
        await Eventually.UntilAsync(() => InventoryStock.GetAsync(factory, skuId), item => item.Reservations.Count == 1 && item.Reservations[0].IsCommitted);
        await using var rowLock = await RowLock.AcquireAsync<InventoryDbContext>(factory, $"SELECT sku_id FROM inventory.stock_items WHERE sku_id = {skuId} FOR UPDATE");

        var setTask = SetAsync(skuId, 6);
        await rowLock.WaitForWaitersAsync(1);
        (await ShipAsync(orderId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await rowLock.WaitForWaitersAsync(2);
        await rowLock.ReleaseAsync();

        (await setTask).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var stock = await Eventually.UntilAsync(() => InventoryStock.GetAsync(factory, skuId), item => item.Reservations.Count == 0);
        // Serial orders: set 6 then ship (6 - 3 = 3) or ship (5 - 3 = 2) then set 6.
        (stock.OnHand is 3 or 6).ShouldBeTrue($"on hand {stock.OnHand}");
    }

    private async Task<(HttpClient Customer, Guid OrderId, Guid SkuId)> ConfirmedOrderAsync(int onHand, int quantity)
    {
        var customer = CustomerClient.Create(factory, Guid.NewGuid());
        var (_, _, skuId) = await BasketApi.CreateSkuAsync(_admin);
        await InventoryStock.SetAsync(factory, skuId, onHand);
        await BasketApi.AddLineAsync(customer, skuId, quantity);
        var orderId = await CheckoutApi.PlaceOrderIdAsync(customer, "tok_visa", 99.90m * quantity);
        await Eventually.UntilAsync(() => CheckoutApi.GetOrderAsync(customer, orderId), order => order.GetProperty("status").GetString() == "Confirmed");
        return (customer, orderId, skuId);
    }

    private static string?[] Statuses(JsonElement order) =>
        [.. order.GetProperty("history").EnumerateArray().Select(change => change.GetProperty("status").GetString())];

    private Task<HttpResponseMessage> ShipAsync(Guid orderId) =>
        _admin.PostAsync($"{AdminOrdersPath}/{orderId}/ship", null, TestContext.Current.CancellationToken);

    private static Task<HttpResponseMessage> CancelAsync(HttpClient customer, Guid orderId) =>
        customer.PostAsync($"{CheckoutApi.OrdersPath}/{orderId}/cancel", null, TestContext.Current.CancellationToken);

    private Task<HttpResponseMessage> SetAsync(Guid skuId, int onHand) =>
        _admin.PutAsJsonAsync($"{StockPath}/{skuId}", new { onHand }, TestContext.Current.CancellationToken);

    // The test's own transaction holds the row lock, so each request queues behind it; counting the queue shows when each one has reached its lock.
    private sealed class RowLock : IAsyncDisposable
    {
        private readonly IServiceScope _scope;
        private readonly IDbContextTransaction _transaction;
        private readonly int _holderProcessId;
        private readonly string _connectionString;

        private RowLock(IServiceScope scope, IDbContextTransaction transaction, int holderProcessId, string connectionString)
        {
            _scope = scope;
            _transaction = transaction;
            _holderProcessId = holderProcessId;
            _connectionString = connectionString;
        }

        public static async Task<RowLock> AcquireAsync<TContext>(ApiFactory factory, FormattableString lockStatement)
            where TContext : DbContext
        {
            var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            var transaction = await db.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
            await db.Database.ExecuteSqlAsync(lockStatement, TestContext.Current.CancellationToken);
            var holderProcessId = await db.Database.SqlQuery<int>($"SELECT pg_backend_pid() AS \"Value\"").SingleAsync(TestContext.Current.CancellationToken);
            var connectionString = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("grit")!;
            return new RowLock(scope, transaction, holderProcessId, connectionString);
        }

        // A request queued behind another one waits on that one's tuple lock, not on this transaction, so the count follows the chain.
        // A separate connection reads pg_stat_activity fresh; this transaction would keep the snapshot of its first read.
        public async Task WaitForWaitersAsync(int count)
        {
            var waiters = await Eventually.UntilAsync(CountQueuedBehindAsync, value => value >= count);
            waiters.ShouldBe(count);
        }

        private async Task<int> CountQueuedBehindAsync()
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                WITH RECURSIVE queued AS (
                    SELECT pid FROM pg_stat_activity WHERE @holder = ANY(pg_blocking_pids(pid))
                    UNION
                    SELECT waiter.pid FROM pg_stat_activity waiter JOIN queued ON queued.pid = ANY(pg_blocking_pids(waiter.pid))
                )
                SELECT count(*)::int FROM queued
                """;
            command.Parameters.AddWithValue("holder", _holderProcessId);
            return (int)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        }

        public Task ReleaseAsync() => _transaction.RollbackAsync(TestContext.Current.CancellationToken);

        public async ValueTask DisposeAsync()
        {
            await _transaction.DisposeAsync();
            _scope.Dispose();
        }
    }
}
