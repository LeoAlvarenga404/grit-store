namespace Grit.Modules.Payments.Infrastructure;

internal sealed class StripeOptions
{
    public const string Section = "Payments:Stripe";

    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;
}
