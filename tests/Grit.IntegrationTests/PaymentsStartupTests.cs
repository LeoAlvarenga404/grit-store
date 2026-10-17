using Grit.Modules.Payments.Domain;
using Grit.Modules.Payments.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

// No test here calls Stripe: building the host only creates the client.
public class PaymentsStartupTests(ApiFactory factory)
{
    [Fact]
    public void ApiStartup_NoProvider_UsesTheFakeGateway() =>
        factory.Services.GetRequiredService<IPaymentGateway>().ShouldBeOfType<FakePaymentGateway>();

    [Fact]
    public async Task ApiStartup_StripeWithBothKeys_UsesTheStripeGateway()
    {
        await using var stripe = factory.WithWebHostBuilder(builder => builder
            .UseSetting("Payments:Provider", "Stripe")
            .UseSetting("Payments:Stripe:SecretKey", "sk_test_placeholder"));

        stripe.Services.GetRequiredService<IPaymentGateway>().ShouldBeOfType<StripePaymentGateway>();
    }

    [Fact]
    public async Task ApiStartup_StripeWithoutSecretKey_FailsNamingTheMissingSetting()
    {
        await using var stripe = factory.WithWebHostBuilder(builder => builder.UseSetting("Payments:Provider", "Stripe"));

        var exception = Should.Throw<Exception>(() => stripe.Services);

        exception.GetBaseException().ShouldBeOfType<InvalidOperationException>().Message.ShouldContain("Payments:Stripe:SecretKey");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ApiStartup_StripeWithoutWebhookSecret_FailsNamingTheSetting(string? webhookSecret)
    {
        await using var stripe = factory.WithWebHostBuilder(builder => builder
            .UseSetting("Payments:Provider", "Stripe")
            .UseSetting("Payments:Stripe:SecretKey", "sk_test_placeholder")
            .UseSetting("Payments:Stripe:WebhookSecret", webhookSecret));

        var exception = Should.Throw<Exception>(() => stripe.Services);

        exception.GetBaseException().ShouldBeOfType<InvalidOperationException>().Message.ShouldContain("Payments:Stripe:WebhookSecret");
    }

    [Fact]
    public async Task ApiStartup_UnknownProvider_FailsNamingTheSetting()
    {
        await using var paypal = factory.WithWebHostBuilder(builder => builder.UseSetting("Payments:Provider", "PayPal"));

        var exception = Should.Throw<Exception>(() => paypal.Services);

        exception.GetBaseException().ShouldBeOfType<InvalidOperationException>().Message.ShouldContain("must be Fake or Stripe");
    }
}
