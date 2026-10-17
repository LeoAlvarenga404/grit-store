using Grit.BuildingBlocks;
using Grit.Modules.Probe.Features.CreateNote;
using Grit.Modules.Probe.Features.Fail;
using Grit.Modules.Probe.Features.GetNote;
using Grit.Modules.Probe.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Grit.Modules.Probe;

public sealed class ProbeModule : IModule
{
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<ProbeDbContext>((services, options) => options.UseNpgsql(
            services.GetRequiredService<NpgsqlDataSource>(),
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ProbeDbContext.Schema)));
    }

    public void AddMessaging(IBusRegistrationConfigurator bus) =>
        bus.AddEntityFrameworkOutbox<ProbeDbContext>(outbox =>
        {
            outbox.QueryDelay = TimeSpan.FromSeconds(1);
            // MassTransit caches lock-statement table names per entity type for the whole process, so with caching on
            // every module would lock against the schema of the first DbContext that resolved OutboxState or InboxState.
            outbox.UsePostgres(enableSchemaCaching: false);
            outbox.UseBusOutbox();
        });

    // The test fixture creates the probe schema with EnsureCreated before the host starts; the probe has no migrations.
    public Task MigrateDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var probe = api.MapGroup("/probe").WithTags("Probe");
        probe.MapFail();

        var notes = probe.MapGroup("/notes");
        notes.MapCreateNote();
        notes.MapGetNote();
    }
}
