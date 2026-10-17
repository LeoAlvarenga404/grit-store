using Grit.BuildingBlocks;
using Grit.Modules.Payments.Domain;
using Grit.Modules.Payments.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Grit.Modules.Payments;

public sealed class PaymentsModule : IModule
{
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
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
}
