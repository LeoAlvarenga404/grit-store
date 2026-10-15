using Grit.Modules.Probe;
using Grit.Modules.Probe.Infrastructure;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Grit.IntegrationTests.ApiFactory))]

namespace Grit.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Same image as the AppHost (one download); tmpfs keeps the throwaway data off the WSL disk.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine")
        .WithTmpfsMount("/var/lib/postgresql")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        await CreateProbeSchemaAsync();
        _ = Server;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:grit", _postgres.GetConnectionString());
        builder.UseSetting("Modules:Additional:0", typeof(ProbeModule).AssemblyQualifiedName);

        // No outbox delivery in this host, so tests read OutboxMessage rows before anything delivers them;
        // OutboxTests turns delivery back on in a host of its own.
        builder.ConfigureTestServices(services =>
        {
            var deliveries = services.Where(IsProbeOutboxDelivery).ToList();
            if (deliveries.Count == 0)
            {
                throw new InvalidOperationException("Outbox delivery service for ProbeDbContext not found; MassTransit registration changed.");
            }

            foreach (var delivery in deliveries)
            {
                services.Remove(delivery);
            }
        });
    }

    private static bool IsProbeOutboxDelivery(ServiceDescriptor service) =>
        service.ImplementationType == typeof(BusOutboxDeliveryService<ProbeDbContext>);

    private async Task CreateProbeSchemaAsync()
    {
        var options = new DbContextOptionsBuilder<ProbeDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
        await using var probe = new ProbeDbContext(options);

        // Runs before the host starts: EnsureCreated does nothing once any table exists, and the API migrates Catalog at start-up.
        await probe.Database.EnsureCreatedAsync();
    }
}
