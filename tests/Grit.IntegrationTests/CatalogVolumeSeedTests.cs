using System.Globalization;
using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.Modules.Catalog.Infrastructure.Seed;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class CatalogVolumeSeedTests(ApiFactory api)
{
    [Fact]
    public async Task DevelopmentStartup_WithVolumeProducts_AddsOnlyTheMissingOnes()
    {
        var connectionString = await api.CreateDatabaseAsync("volume_seed");
        await using (var first = CreateHost(connectionString, volumeProducts: 1200))
        {
            (await CountProductsAsync(first)).ShouldBe(1200);
        }

        await using var second = CreateHost(connectionString, volumeProducts: 1500);
        (await CountProductsAsync(second)).ShouldBe(1500);

        await using var scope = second.Services.CreateAsyncScope();
        var added = await scope.ServiceProvider.GetRequiredService<VolumeSeeder>().SeedAsync(1500, TestContext.Current.CancellationToken);
        added.ShouldBe(0);
    }

    [Fact]
    public async Task DefaultStartup_SeedsNoVolumeProducts()
    {
        var categoryIds = VolumeCatalog.CategoryNames.ToDictionary(name => name, _ => CategoryId.New());
        var firstSlug = new VolumeCatalog(categoryIds).Generate(0).Slug;

        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        (await db.Products.AnyAsync(product => product.Slug == firstSlug, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    private WebApplicationFactory<Program> CreateHost(string connectionString, int volumeProducts) =>
        api.WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:grit", connectionString)
            .UseSetting("Seed:VolumeProducts", volumeProducts.ToString(CultureInfo.InvariantCulture)));

    // Reading Services starts the host, so the seed has finished before the count.
    private static async Task<int> CountProductsAsync(WebApplicationFactory<Program> host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Products.CountAsync(TestContext.Current.CancellationToken);
    }
}
