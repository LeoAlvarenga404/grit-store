using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.Modules.Catalog.Infrastructure.Seed;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

// Each test seeds a database of its own from a seed folder written to the temp directory.
public class CatalogSeedImageTests(ApiFactory api, ProductImageHost images) : IClassFixture<ProductImageHost>
{
    private static readonly byte[] CoverBytes = [0x52, 0x49, 0x46, 0x46, 0x01, 0x02, 0x03];

    [Fact]
    public async Task SeedAsync_WithStorage_UploadsTheImageFilesThatExistAndAttachesThem()
    {
        await using var host = await HostAsync(images.Host, "seed_images");
        var path = await WriteSeedFolderAsync();

        await SeedAsync(host, path);

        var product = await ProductAsync(host);
        var key = Product.ImageKeyPrefix(product.Id) + "capa.webp";
        product.ImageKeys.ShouldHaveSingleItem().ShouldBe(key);
        var stored = await host.Services.GetRequiredService<ProductImageStorage>().FindAsync(key, TestContext.Current.CancellationToken);
        stored.ShouldNotBeNull();
        stored.Value.SizeInBytes.ShouldBe(CoverBytes.LongLength);
        stored.Value.ContentType.ShouldBe("image/webp");
    }

    [Fact]
    public async Task SeedAsync_WithoutStorage_SeedsTheProductWithoutImages()
    {
        await using var host = await HostAsync(api, "seed_images_no_storage");
        var path = await WriteSeedFolderAsync();

        await SeedAsync(host, path);

        (await ProductAsync(host)).ImageKeys.ShouldBeEmpty();
    }

    // Reading Services starts the host: migrations run and, with the storage, the bucket is created.
    private async Task<WebApplicationFactory<Program>> HostAsync(WebApplicationFactory<Program> parent, string database)
    {
        var connectionString = await api.CreateDatabaseAsync(database);
        var host = parent.WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:grit", connectionString));
        _ = host.Services;
        return host;
    }

    // "falta.webp" is listed but has no file: the seeder skips it instead of failing.
    private static async Task<string> WriteSeedFolderAsync()
    {
        var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grit-seed-{Guid.NewGuid():N}"));
        var imagesFolder = folder.CreateSubdirectory("images");
        await File.WriteAllBytesAsync(Path.Combine(imagesFolder.FullName, "capa.webp"), CoverBytes, TestContext.Current.CancellationToken);
        var path = Path.Combine(folder.FullName, "catalog.json");
        await File.WriteAllTextAsync(path, """
            {"categories": [{"name": "Camping", "products": [{"name": "Lanterna de Cabeça", "description": "Leve.", "attributes": {},
              "skus": [{"attributes": {"cor": "preto"}, "priceAmount": 89.90}], "images": ["capa.webp", "falta.webp"]}]}]}
            """, TestContext.Current.CancellationToken);
        return path;
    }

    private static async Task SeedAsync(WebApplicationFactory<Program> host, string path)
    {
        await using var scope = host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CatalogSeeder>().SeedAsync(path, TestContext.Current.CancellationToken);
    }

    private static async Task<Product> ProductAsync(WebApplicationFactory<Program> host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        return await db.Products.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
    }
}
