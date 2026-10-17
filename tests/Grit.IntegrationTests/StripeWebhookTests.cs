using System.Net;
using System.Security.Cryptography;
using System.Text;
using Grit.Modules.Payments.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class StripeWebhookTests(ApiFactory factory)
{
    private const string WebhookPath = "/api/v1/payments/stripe/webhook";

    [Fact]
    public async Task Webhook_ValidSignature_Returns200AndRecordsTheEvent()
    {
        var eventId = NewEventId();

        var response = await PostAsync(factory.CreateClient(), EventJson(eventId), ApiFactory.StripeWebhookSecret, DateTimeOffset.UtcNow);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await CountAsync(eventId)).ShouldBe(1);
    }

    // Stripe redelivers until it gets a 2xx, so a replay answers 200 and records nothing new.
    [Fact]
    public async Task Webhook_SameEventTwice_RecordsItOnce()
    {
        var eventId = NewEventId();
        var client = factory.CreateClient();

        var first = await PostAsync(client, EventJson(eventId), ApiFactory.StripeWebhookSecret, DateTimeOffset.UtcNow);
        var replay = await PostAsync(client, EventJson(eventId), ApiFactory.StripeWebhookSecret, DateTimeOffset.UtcNow);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await CountAsync(eventId)).ShouldBe(1);
    }

    [Fact]
    public async Task Webhook_SignedWithAnotherSecret_Returns400AndRecordsNothing()
    {
        var eventId = NewEventId();

        var response = await PostAsync(factory.CreateClient(), EventJson(eventId), "whsec_not_the_configured_one", DateTimeOffset.UtcNow);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("payments.invalid_signature");
        (await CountAsync(eventId)).ShouldBe(0);
    }

    [Fact]
    public async Task Webhook_SignedTenMinutesAgo_Returns400()
    {
        var signedAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        var response = await PostAsync(factory.CreateClient(), EventJson(NewEventId()), ApiFactory.StripeWebhookSecret, signedAt);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("payments.invalid_signature");
    }

    [Fact]
    public async Task Webhook_WithoutSignature_Returns400()
    {
        var response = await PostAsync(factory.CreateClient(), EventJson(NewEventId()), secret: null, DateTimeOffset.UtcNow);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("payments.invalid_signature");
    }

    // An empty secret would accept events signed with an empty key, which anyone can compute.
    [Fact]
    public async Task Webhook_NoSecretConfigured_Returns404EvenForAnEventSignedWithAnEmptyKey()
    {
        await using var unconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting("Payments:Stripe:WebhookSecret", string.Empty));

        var response = await PostAsync(unconfigured.CreateClient(), EventJson(NewEventId()), string.Empty, DateTimeOffset.UtcNow);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static string NewEventId() => $"evt_{Guid.NewGuid():N}";

    // api_version older than the library's on purpose: the Stripe CLI forwards events in the account's version.
    private static string EventJson(string eventId) =>
        $$$$"""{"id":"{{{{eventId}}}}","object":"event","api_version":"2020-08-27","created":1792200000,"type":"payment_intent.succeeded","livemode":false,"pending_webhooks":1,"data":{"object":{"id":"pi_3Q8webhook","object":"payment_intent"}}}""";

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string json, string? secret, DateTimeOffset signedAt)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        if (secret is not null)
        {
            request.Headers.TryAddWithoutValidation("Stripe-Signature", SignatureHeader(json, secret, signedAt));
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    // Stripe's scheme, computed here without the library: t=<unix seconds>,v1=hex(HMAC-SHA256(secret, "<t>.<payload>")).
    private static string SignatureHeader(string json, string secret, DateTimeOffset signedAt)
    {
        var timestamp = signedAt.ToUnixTimeSeconds();
        var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{json}"));
        return $"t={timestamp},v1={Convert.ToHexStringLower(signature)}";
    }

    private async Task<int> CountAsync(string eventId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        return await db.StripeEvents.CountAsync(stripeEvent => stripeEvent.EventId == eventId, TestContext.Current.CancellationToken);
    }
}
