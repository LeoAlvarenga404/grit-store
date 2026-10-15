using Grit.BuildingBlocks;
using Grit.Modules.Catalog.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Grit.Modules.Catalog;

public sealed class CatalogModule : IModule
{
    private static readonly AuditInterceptor Audit = new();

    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<CatalogDbContext>((services, options) =>
            CatalogDbContext.Configure(options, services.GetRequiredService<NpgsqlDataSource>()).AddInterceptors(Audit));
    }

    // Catalog publishes no integration event yet, and the single bus-outbox slot stays with the probe (ADR 0005).
    public void AddMessaging(IBusRegistrationConfigurator bus)
    {
    }

    public async Task MigrateDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync(cancellationToken);
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
    }
}
