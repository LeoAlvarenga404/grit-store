using Grit.BuildingBlocks;
using Grit.Modules.Payments.Domain;
using Grit.Modules.Payments.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;

namespace Grit.Modules.Payments;

public sealed class PaymentsModule : IModule
{
    private const string ProviderKey = "Payments:Provider";

    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddGateway(builder);
    }

    public void AddMessaging(IBusRegistrationConfigurator bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        bus.AddConsumer<PaymentConsumer>();
    }

    // No payments schema until Stripe webhooks need one.
    public Task MigrateDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
    }

    // Fake unless configured: tests, the build-time OpenAPI run and a fresh clone need no Stripe account.
    private static void AddGateway(IHostApplicationBuilder builder)
    {
        var provider = builder.Configuration[ProviderKey] ?? "Fake";
        if (provider == "Fake")
        {
            builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
            return;
        }

        if (provider != "Stripe")
        {
            throw new InvalidOperationException($"{ProviderKey} must be Fake or Stripe, not '{provider}'.");
        }

        var stripe = builder.Configuration.GetSection(StripeOptions.Section).Get<StripeOptions>() ?? new StripeOptions();
        if (string.IsNullOrWhiteSpace(stripe.SecretKey) || string.IsNullOrWhiteSpace(stripe.WebhookSecret))
        {
            throw new InvalidOperationException(
                $"{ProviderKey}=Stripe needs {StripeOptions.Section}:SecretKey and {StripeOptions.Section}:WebhookSecret (user secrets or environment).");
        }

        builder.Services.AddSingleton(new StripeClient(stripe.SecretKey));
        builder.Services.AddSingleton<IPaymentGateway, StripePaymentGateway>();
    }
}
