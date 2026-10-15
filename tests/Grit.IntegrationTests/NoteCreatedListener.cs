using Grit.Modules.Probe.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

internal sealed class NoteCreatedListener : IAsyncDisposable
{
    private readonly TaskCompletionSource<NoteCreated> _received = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly HostReceiveEndpointHandle _handle;

    private NoteCreatedListener(IBus bus, string text)
    {
        _handle = bus.ConnectReceiveEndpoint($"note-created-{Guid.NewGuid():N}", endpoint =>
            endpoint.Handler<NoteCreated>(context =>
            {
                if (context.Message.Text == text)
                {
                    _received.TrySetResult(context.Message);
                }

                return Task.CompletedTask;
            }));
    }

    public Task<NoteCreated> Received => _received.Task;

    public static async Task<NoteCreatedListener> StartAsync(WebApplicationFactory<Program> factory, string text)
    {
        var listener = new NoteCreatedListener(factory.Services.GetRequiredService<IBus>(), text);
        await listener._handle.Ready;
        return listener;
    }

    public async ValueTask DisposeAsync() => await _handle.StopAsync();
}
