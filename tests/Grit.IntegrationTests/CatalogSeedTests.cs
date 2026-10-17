using System.Net;
using Grit.Modules.Catalog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Grit.IntegrationTests;

// Each test has a database of its own: the counts must see only what the seed wrote.
public class CatalogSeedTests(ApiFactory api)
{
    internal static readonly string SeedPath = Path.Combine(AppContext.BaseDirectory, "Seed", "catalog.json");

    internal static readonly (int Categories, int Products, int Skus) SeedCounts = (4, 16, 30);

    [Fact]
    public async Task DevelopmentStartup_TwiceWithSeedPath_SeedsTheCatalogOnce()
    {
        var connectionString = await api.CreateDatabaseAsync("seed_twice");

        await using (var first = CreateHost(connectionString, Environments.Development, SeedPath))
        {
            (await CountAsync(first)).ShouldBe(SeedCounts);
            var page = await first.CreateClient().GetAsync("/api/v1/catalog/products/barraca-iglu-2-pessoas", TestContext.Current.CancellationToken);
            page.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        await using var second = CreateHost(connectionString, Environments.Development, SeedPath);

        (await CountAsync(second)).ShouldBe(SeedCounts);
    }

    [Fact]
    public async Task ProductionStartup_WithSeedPath_SeedsNothing()
    {
        var connectionString = await api.CreateDatabaseAsync("seed_production");
        await using (var migrated = CreateHost(connectionString, Environments.Development, seedPath: null))
        {
            (await CountAsync(migrated)).ShouldBe((0, 0, 0));
        }

        await using var production = CreateHost(connectionString, Environments.Production, SeedPath);

        (await CountAsync(production)).ShouldBe((0, 0, 0));
    }

    private WebApplicationFactory<Program> CreateHost(string connectionString, string environment, string? seedPath) =>
        api.WithWebHostBuilder(builder => builder
            .UseEnvironment(environment)
            .UseSetting("ConnectionStrings:grit", connectionString)
            .UseSetting("Seed:Path", seedPath));

    // Reading Services starts the host, so every hosted service has finished StartAsync before the counts are read.
    internal static async Task<(int Categories, int Products, int Skus)> CountAsync(WebApplicationFactory<Program> host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var cancellationToken = TestContext.Current.CancellationToken;
        return (
            await db.Categories.CountAsync(cancellationToken),
            await db.Products.CountAsync(cancellationToken),
            await db.Products.SelectMany(product => product.Skus).CountAsync(cancellationToken));
    }
}
