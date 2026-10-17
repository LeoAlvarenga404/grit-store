namespace Grit.Modules.Payments.Infrastructure;

internal sealed record StripeEvent(string EventId, string Type, DateTimeOffset ReceivedAt);
