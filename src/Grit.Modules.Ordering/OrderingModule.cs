using System.Data;
using Grit.BuildingBlocks;
using Grit.Modules.Ordering.Features.CancelOrder;
using Grit.Modules.Ordering.Features.GetOrder;
using Grit.Modules.Ordering.Features.ListOrders;
using Grit.Modules.Ordering.Features.PlaceOrder;
using Grit.Modules.Ordering.Infrastructure;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Grit.Modules.Ordering;

public sealed class OrderingModule : IModule
{
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<OrderingDbContext>((services, options) =>
            OrderingDbContext.Configure(options, services.GetRequiredService<NpgsqlDataSource>()));
        builder.Services.TryAddSingleton(TimeProvider.System);

        // UseBusOutbox binds the generic IPublishEndpoint to one DbContext per bus, and the probe claims it in tests (ADR 0005).
        // Ordering's handlers publish through this provider instead, which always writes to Ordering's own outbox.
        builder.Services.AddScoped<EntityFrameworkScopedBusContextProvider<IBus, OrderingDbContext>>();
    }

    public void AddMessaging(IBusRegistrationConfigurator bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        bus.AddEntityFrameworkOutbox<OrderingDbContext>(outbox =>
        {
            outbox.QueryDelay = TimeSpan.FromSeconds(1);
            outbox.IsolationLevel = IsolationLevel.ReadCommitted;
            outbox.UsePostgres();
            outbox.UseBusOutbox();
        });
    }

    public async Task MigrateDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().Database.MigrateAsync(cancellationToken);
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var orders = api.MapGroup("/orders").WithTags("Ordering").RequireAuthorization();
        orders.MapPlaceOrder();
        orders.MapListOrders();
        orders.MapGetOrder();
        orders.MapCancelOrder();
    }
}
