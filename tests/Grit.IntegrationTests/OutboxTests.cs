using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Grit.Modules.Probe.Infrastructure;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

// Same collection as TransactionTests: the delivering host below would otherwise delete their outbox rows mid-test.
[Collection("Outbox")]
public class OutboxTests(ApiFactory factory)
{
    [Fact]
    public async Task CreateNote_PublishesNoteCreatedThroughTheOutbox()
    {
        await using var deliveringFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddHostedService<BusOutboxDeliveryService<ProbeDbContext>>()));
        var text = $"outbox-{Guid.NewGuid():N}";
        await using var listener = await NoteCreatedListener.StartAsync(deliveringFactory, text);

        var response = await deliveringFactory.CreateClient().PostAsJsonAsync("/api/v1/probe/notes", new { text }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetGuid();
        var message = await listener.Received.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        message.NoteId.ShouldBe(id);
        message.Text.ShouldBe(text);
    }
}
