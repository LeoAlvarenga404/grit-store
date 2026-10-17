using System.Collections.Concurrent;
using Grit.Modules.Payments.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

// FakePaymentGateway.RefundAsync does nothing, so tests observe the refund as the message the saga publishes.
internal sealed class PaymentListener : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, string> _authorizations = new();
    private readonly ConcurrentDictionary<Guid, string> _refunds = new();
    private readonly HostReceiveEndpointHandle _handle;

    private PaymentListener(IBus bus)
    {
        _handle = bus.ConnectReceiveEndpoint($"payment-listener-{Guid.NewGuid():N}", endpoint =>
        {
            endpoint.Handler<PaymentAuthorized>(context => Record(_authorizations, context.Message.OrderId, context.Message.AuthorizationId));
            endpoint.Handler<RefundPayment>(context => Record(_refunds, context.Message.OrderId, context.Message.AuthorizationId));
        });
    }

    public static async Task<PaymentListener> StartAsync(WebApplicationFactory<Program> factory)
    {
        var listener = new PaymentListener(factory.Services.GetRequiredService<IBus>());
        await listener._handle.Ready;
        return listener;
    }

    public string? AuthorizationOf(Guid orderId) => _authorizations.GetValueOrDefault(orderId);

    public string? RefundOf(Guid orderId) => _refunds.GetValueOrDefault(orderId);

    public async ValueTask DisposeAsync() => await _handle.StopAsync();

    private static Task Record(ConcurrentDictionary<Guid, string> messages, Guid orderId, string authorizationId)
    {
        messages[orderId] = authorizationId;
        return Task.CompletedTask;
    }
}
