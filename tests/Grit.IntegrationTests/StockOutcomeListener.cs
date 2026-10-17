using System.Collections.Concurrent;
using Grit.Modules.Inventory.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

internal sealed class StockOutcomeListener : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, bool> _outcomes = new();
    private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IReadOnlyCollection<Guid> _orderIds;
    private readonly HostReceiveEndpointHandle _handle;

    private StockOutcomeListener(IBus bus, IReadOnlyCollection<Guid> orderIds)
    {
        _orderIds = orderIds;
        _handle = bus.ConnectReceiveEndpoint($"stock-outcome-{Guid.NewGuid():N}", endpoint =>
        {
            endpoint.Handler<StockReserved>(context => Record(context.Message.OrderId, isReserved: true));
            endpoint.Handler<StockReservationFailed>(context => Record(context.Message.OrderId, isReserved: false));
        });
    }

    public IReadOnlyDictionary<Guid, bool> Outcomes => _outcomes;

    public Task Completed => _completed.Task;

    public static async Task<StockOutcomeListener> StartAsync(WebApplicationFactory<Program> factory, IReadOnlyCollection<Guid> orderIds)
    {
        var listener = new StockOutcomeListener(factory.Services.GetRequiredService<IBus>(), orderIds);
        await listener._handle.Ready;
        return listener;
    }

    public async ValueTask DisposeAsync() => await _handle.StopAsync();

    private Task Record(Guid orderId, bool isReserved)
    {
        if (_orderIds.Contains(orderId) && _outcomes.TryAdd(orderId, isReserved) && _outcomes.Count == _orderIds.Count)
        {
            _completed.TrySetResult();
        }

        return Task.CompletedTask;
    }
}
