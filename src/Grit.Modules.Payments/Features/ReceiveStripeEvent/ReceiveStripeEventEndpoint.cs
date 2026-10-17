using Grit.BuildingBlocks;
using Grit.Modules.Payments.Domain;
using Grit.Modules.Payments.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace Grit.Modules.Payments.Features.ReceiveStripeEvent;

// The saga learns the payment result from the synchronous PaymentIntent call (the gateway declines anything that would
// need the customer back), so events only leave a trail: each one is stored once and logged; a dispute is a warning.
internal static partial class ReceiveStripeEventEndpoint
{
    public static void MapReceiveStripeEvent(this IEndpointRouteBuilder payments) =>
        payments.MapPost("/stripe/webhook", ReceiveStripeEvent)
            .ExcludeFromDescription();

    private static async Task<Results<Ok, NotFound, ProblemHttpResult>> ReceiveStripeEvent(
        HttpRequest request,
        IOptions<StripeOptions> options,
        PaymentsDbContext db,
        ILoggerFactory loggers,
        CancellationToken cancellationToken)
    {
        // With no secret any sender could sign an event, so the endpoint does not exist until one is configured.
        var secret = options.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            return TypedResults.NotFound();
        }

        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);
        Event stripeEvent;
        try
        {
            // The Stripe CLI and the dashboard send the account's API version, which may differ from the library's.
            stripeEvent = EventUtility.ConstructEvent(json, request.Headers["Stripe-Signature"].ToString(), secret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            return PaymentsErrors.InvalidSignature.ToProblem();
        }

        if (await db.RecordStripeEventAsync(stripeEvent.Id, stripeEvent.Type, cancellationToken))
        {
            var level = stripeEvent.Type == EventTypes.ChargeDisputeCreated ? LogLevel.Warning : LogLevel.Information;
            var logger = loggers.CreateLogger(typeof(ReceiveStripeEventEndpoint));
            LogEvent(logger, level, stripeEvent.Id, stripeEvent.Type);
        }

        return TypedResults.Ok();
    }

    [LoggerMessage(Message = "Stripe event {EventId} of type {EventType} received")]
    private static partial void LogEvent(ILogger logger, LogLevel level, string eventId, string eventType);
}
