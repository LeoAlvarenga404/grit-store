using System.Data;
using Grit.BuildingBlocks;
using Grit.Modules.Inventory.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Grit.Modules.Inventory;

public sealed class InventoryModule : IModule
{
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<InventoryDbContext>((services, options) =>
            InventoryDbContext.Configure(options, services.GetRequiredService<NpgsqlDataSource>()));
        builder.Services.TryAddSingleton(TimeProvider.System);

        // Same switch as the catalog seed: the AppHost sets Seed:Path in Development; tests never do and stock their own SKUs.
        if (builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(builder.Configuration["Seed:Path"]))
        {
            builder.Services.AddHostedService<DevelopmentStockSeeder>();
        }
    }

    // Consumer outbox only: Inventory publishes from consumers, never from an HTTP command, so it needs no bus outbox (ADR 0005).
    public void AddMessaging(IBusRegistrationConfigurator bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        bus.AddEntityFrameworkOutbox<InventoryDbContext>(outbox =>
        {
            outbox.IsolationLevel = IsolationLevel.ReadCommitted;
            outbox.UsePostgres();
        });
        bus.AddConsumer<StockConsumer, StockConsumerDefinition>();
    }

    public async Task MigrateDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync(cancellationToken);
    }

    // Stock adjustment endpoints arrive with the admin (5a).
    public void MapEndpoints(IEndpointRouteBuilder api)
    {
    }
}
