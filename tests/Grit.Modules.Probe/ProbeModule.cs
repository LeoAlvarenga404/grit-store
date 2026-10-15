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
            outbox.UsePostgres();
            outbox.UseBusOutbox();
        });

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var probe = api.MapGroup("/probe").WithTags("Probe");
        probe.MapFail();

        var notes = probe.MapGroup("/notes");
        notes.MapCreateNote();
        notes.MapGetNote();
    }
}
