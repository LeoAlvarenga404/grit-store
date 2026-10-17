using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Grit.Modules.Catalog.Infrastructure.Seed;

// Hosted services start inside app.Run(), after Program.cs applied the migrations (ADR 0008).
internal sealed partial class CatalogSeedService(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    ILogger<CatalogSeedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var path = configuration["Seed:Path"];
        if (!string.IsNullOrWhiteSpace(path))
        {
            var report = await scope.ServiceProvider.GetRequiredService<CatalogSeeder>().SeedAsync(path, cancellationToken);
            LogSeeded(logger, report.CategoriesCreated, report.ProductsCreated, path);
        }

        // Off by default: generated products only for load tests (bench/), added after the hand-written seed.
        var volumeProducts = configuration.GetValue("Seed:VolumeProducts", 0);
        if (volumeProducts <= 0)
        {
            return;
        }

        var added = await scope.ServiceProvider.GetRequiredService<VolumeSeeder>().SeedAsync(volumeProducts, cancellationToken);
        LogVolumeSeeded(logger, added, volumeProducts);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Seed added {CategoriesCreated} categories and {ProductsCreated} products from {Path}")]
    private static partial void LogSeeded(ILogger logger, int categoriesCreated, int productsCreated, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Volume seed added {ProductsCreated} of {VolumeProducts} generated products")]
    private static partial void LogVolumeSeeded(ILogger logger, int productsCreated, int volumeProducts);
}
