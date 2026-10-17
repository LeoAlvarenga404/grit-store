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

    private static readonly byte[] BlueBytes = [0x52, 0x49, 0x46, 0x46, 0x09];

    [Fact]
    public async Task SeedAsync_WithStorage_UploadsTheImageFilesThatExistAndAttachesThem()
    {
        await using var host = await HostAsync(images.Host, "seed_images");
        var path = await WriteSeedFolderAsync();

        await SeedAsync(host, path);

        var product = await ProductAsync(host);
        var prefix = Product.ImageKeyPrefix(product.Id);
        product.Images.ShouldBe([new ProductImage(prefix + "capa.webp", "preto"), new ProductImage(prefix + "azul.webp", "azul")]);
        var storage = host.Services.GetRequiredService<ProductImageStorage>();
        var stored = await storage.FindAsync(prefix + "capa.webp", TestContext.Current.CancellationToken);
        stored.ShouldNotBeNull();
        stored.Value.SizeInBytes.ShouldBe(CoverBytes.LongLength);
        stored.Value.ContentType.ShouldBe("image/webp");
        var blue = await storage.FindAsync(prefix + "azul.webp", TestContext.Current.CancellationToken);
        blue.ShouldNotBeNull();
        blue.Value.SizeInBytes.ShouldBe(BlueBytes.LongLength);
    }

    [Fact]
    public async Task SeedAsync_WithoutStorage_SeedsTheProductWithoutImages()
    {
        await using var host = await HostAsync(api, "seed_images_no_storage");
        var path = await WriteSeedFolderAsync();

        await SeedAsync(host, path);

        (await ProductAsync(host)).Images.ShouldBeEmpty();
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
        await File.WriteAllBytesAsync(Path.Combine(imagesFolder.FullName, "azul.webp"), BlueBytes, TestContext.Current.CancellationToken);
        var path = Path.Combine(folder.FullName, "catalog.json");
        await File.WriteAllTextAsync(path, """
            {"categories": [{"name": "Camping", "products": [{"name": "Lanterna de Cabeça", "description": "Leve.", "attributes": {},
              "skus": [{"attributes": {"cor": "preto"}, "priceAmount": 89.90}, {"attributes": {"cor": "azul"}, "priceAmount": 89.90}],
              "images": [{"color": "preto", "files": ["capa.webp", "falta.webp"]}, {"color": "azul", "files": ["azul.webp"]}]}]}]}
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
