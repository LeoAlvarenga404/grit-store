using Grit.Modules.Probe;
using Grit.Modules.Probe.Infrastructure;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

[assembly: AssemblyFixture(typeof(Grit.IntegrationTests.ApiFactory))]

namespace Grit.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Test-only key, never used outside the test host.
    public const string JwtSigningKey = "test-only-signing-key-0123456789abcdef";

    // Same image as the AppHost (one download); tmpfs keeps the throwaway data off the WSL disk.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine")
        .WithTmpfsMount("/var/lib/postgresql")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8.10.2-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
        await CreateProbeSchemaAsync(_postgres.GetConnectionString());
        _ = Server;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }

    // A database of its own on the same container, for tests that count rows no other test may add to.
    public async Task<string> CreateDatabaseAsync(string name)
    {
        await using (var dataSource = NpgsqlDataSource.Create(_postgres.GetConnectionString()))
        await using (var command = dataSource.CreateCommand($"CREATE DATABASE {name}"))
        {
            await command.ExecuteNonQueryAsync();
        }

        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = name }.ConnectionString;
        await CreateProbeSchemaAsync(connectionString);
        return connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:grit", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:redis", _redis.GetConnectionString());
        builder.UseSetting("Modules:Additional:0", typeof(ProbeModule).AssemblyQualifiedName);
        builder.UseSetting("Identity:Jwt:SigningKey", JwtSigningKey);
        builder.UseSetting("Identity:LoginAttemptsPerMinute", "1000");

        // Every host here shares one Redis, so a read cached from one test database would answer another;
        // CatalogReadCacheTests turns the cache on in a host of its own.
        builder.UseSetting("Caching:Enabled", "false");

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

    private static async Task CreateProbeSchemaAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ProbeDbContext>().UseNpgsql(connectionString).Options;
        await using var probe = new ProbeDbContext(options);

        // Runs before the host starts: EnsureCreated does nothing once any table exists, and the API migrates Catalog at start-up.
        await probe.Database.EnsureCreatedAsync();
    }
}
