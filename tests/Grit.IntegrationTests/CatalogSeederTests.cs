using System.Globalization;
using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure;
using Grit.Modules.Catalog.Infrastructure.Seed;
using Grit.SharedKernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Grit.IntegrationTests;

// Each test has a database of its own and calls the seeder directly, so its exception and report are visible.
public class CatalogSeederTests(ApiFactory api)
{
    private const string RemovedSlug = "tenis-de-trilha-garra";

    [Fact]
    public async Task SeedAsync_BadPriceForAProductAlreadySeeded_ThrowsAndChangesNothing()
    {
        await using var host = await HostAsync("seed_bad_price", CatalogSeedTests.SeedPath);
        var before = await SnapshotAsync(host);
        var path = await WriteSeedFileAsync("""
            {"categories": [{"name": "Calçados", "products": [{"name": "Tênis de Corrida Asfalto Leve", "description": "Leve.",
              "attributes": {}, "skus": [{"attributes": {"cor": "azul"}, "priceAmount": -1}]}]}]}
            """);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => SeedAsync(host, path));

        exception.Message.ShouldBe("The seed entry 'Tênis de Corrida Asfalto Leve' is invalid: money.negative_amount.");
        (await SnapshotAsync(host)).ShouldBe(before);
    }

    [Fact]
    public async Task SeedAsync_SlugRepeatedInTheFile_Throws()
    {
        await using var host = await HostAsync("seed_repeated_slug", CatalogSeedTests.SeedPath);
        var path = await WriteSeedFileAsync("""
            {"categories": [{"name": "Calçados", "products": [
              {"name": "Tênis de Corrida Asfalto Leve", "description": "Leve.", "attributes": {}, "skus": []},
              {"name": "Tênis de corrida, asfalto leve", "description": "Leve.", "attributes": {}, "skus": []}]}]}
            """);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => SeedAsync(host, path));

        exception.Message.ShouldBe("The seed entry 'Tênis de corrida, asfalto leve' repeats the slug 'tenis-de-corrida-asfalto-leve'.");
    }

    [Fact]
    public async Task SeedAsync_BadEntryAfterValidOnes_PersistsNothing()
    {
        await using var host = await HostAsync("seed_atomic", seedPath: null);
        var path = await WriteSeedFileAsync("""
            {"categories": [
              {"name": "Camping", "products": [{"name": "Lanterna de Cabeça", "description": "Leve.", "attributes": {},
                "skus": [{"attributes": {"cor": "preto"}, "priceAmount": 89.90}]}]},
              {"name": "Trilha", "products": [{"name": "Bastão de Caminhada", "description": "Leve.", "attributes": {},
                "skus": [{"attributes": {"cor": "preto"}, "priceAmount": 129.90}, {"attributes": {"cor": "preto"}, "priceAmount": 139.90}]}]}]}
            """);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => SeedAsync(host, path));

        exception.Message.ShouldBe("The seed entry 'Bastão de Caminhada' is invalid: catalog.duplicate_variant.");
        (await CatalogSeedTests.CountAsync(host)).ShouldBe((0, 0, 0));
    }

    [Fact]
    public async Task SeedAsync_PartlySeededCatalog_AddsOnlyTheMissingProductAndKeepsTheRest()
    {
        await using var host = await HostAsync("seed_partial", CatalogSeedTests.SeedPath);
        await RenameRepriceAndRemoveAsync(host);
        var before = await SnapshotAsync(host);

        var report = await SeedAsync(host, CatalogSeedTests.SeedPath);

        report.ShouldBe(new SeedReport(0, 1));
        var after = await SnapshotAsync(host);
        after.Where(line => !line.StartsWith(RemovedSlug + "|", StringComparison.Ordinal)).ShouldBe(before);
        after.Count(line => line.StartsWith(RemovedSlug + "|", StringComparison.Ordinal)).ShouldBe(1);
        (await CatalogSeedTests.CountAsync(host)).ShouldBe(CatalogSeedTests.SeedCounts);
    }

    private const string ImportedSeed = """
        {"categories": [{"name": "Tênis", "products": [{"name": "Tênis Veloz", "description": "Leve.", "attributes": {},
          "skus": [{"attributes": {"cor": "preto", "tamanho": "40"}, "priceAmount": 299.90}]}]}]}
        """;

    [Fact]
    public async Task SeedAsync_OwnLineFolderNextToTheSeed_SeedsBothFilesInOneRun()
    {
        await using var host = await HostAsync("seed_own_line", seedPath: null);
        var path = await WriteSeedFolderAsync(ImportedSeed, OwnLine(89.90m));

        var report = await SeedAsync(host, path);

        report.ShouldBe(new SeedReport(2, 2));
        (await CatalogSeedTests.CountAsync(host)).ShouldBe((2, 2, 3));
    }

    [Fact]
    public async Task SeedAsync_BadEntryInTheOwnLine_PersistsNothingFromEitherFile()
    {
        await using var host = await HostAsync("seed_own_line_bad", seedPath: null);
        var path = await WriteSeedFolderAsync(ImportedSeed, OwnLine(-1m));

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => SeedAsync(host, path));

        exception.Message.ShouldBe("The seed entry 'Camiseta GRIT Teste' is invalid: money.negative_amount.");
        (await CatalogSeedTests.CountAsync(host)).ShouldBe((0, 0, 0));
    }

    [Fact]
    public async Task SeedAsync_CategoryInBothFiles_Throws()
    {
        await using var host = await HostAsync("seed_own_line_repeated", seedPath: null);
        var path = await WriteSeedFolderAsync(ImportedSeed, OwnLine(89.90m).Replace("Camisetas e regatas", "Tênis", StringComparison.Ordinal));

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => SeedAsync(host, path));

        exception.Message.ShouldBe("The seed entry 'Tênis' repeats the slug 'tenis'.");
    }

    // Two SKUs and a photo that does not exist: the own line seeds without its pictures until the user adds them.
    private static string OwnLine(decimal price) => $$"""
        {"categories": [{"name": "Camisetas e regatas", "products": [{"name": "Camiseta GRIT Teste", "description": "Leve.",
          "attributes": {"marca": "GRIT"},
          "skus": [{"attributes": {"cor": "preto", "tamanho": "P"}, "priceAmount": {{price.ToString(CultureInfo.InvariantCulture)}}},
                   {"attributes": {"cor": "preto", "tamanho": "M"}, "priceAmount": {{price.ToString(CultureInfo.InvariantCulture)}}}],
          "images": [{"color": "preto", "files": ["camiseta-grit-teste-preto-1.webp"]}]}]}]}
        """;

    private static async Task<string> WriteSeedFolderAsync(string seed, string ownLine)
    {
        var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grit-seed-{Guid.NewGuid():N}"));
        var ownLineFolder = folder.CreateSubdirectory(CatalogSeeder.OwnLineFolder);
        await File.WriteAllTextAsync(Path.Combine(ownLineFolder.FullName, "catalog.json"), ownLine, TestContext.Current.CancellationToken);
        var path = Path.Combine(folder.FullName, "catalog.json");
        await File.WriteAllTextAsync(path, seed, TestContext.Current.CancellationToken);
        return path;
    }

    // Reading Services starts the host: migrations run and, with a seed path, the catalog is seeded first.
    private async Task<WebApplicationFactory<Program>> HostAsync(string database, string? seedPath)
    {
        var connectionString = await api.CreateDatabaseAsync(database);
        var host = api.WithWebHostBuilder(builder => builder
            .UseEnvironment(Environments.Development)
            .UseSetting("ConnectionStrings:grit", connectionString)
            .UseSetting("Seed:Path", seedPath));
        _ = host.Services;
        return host;
    }

    private static async Task<SeedReport> SeedAsync(WebApplicationFactory<Program> host, string path)
    {
        await using var scope = host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CatalogSeeder>().SeedAsync(path, TestContext.Current.CancellationToken);
    }

    private static async Task<string> WriteSeedFileAsync(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"grit-seed-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json, TestContext.Current.CancellationToken);
        return path;
    }

    private static async Task RenameRepriceAndRemoveAsync(WebApplicationFactory<Program> host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var products = await db.Products.Include(product => product.Skus).ToListAsync(TestContext.Current.CancellationToken);
        var tent = products.Single(product => product.Slug.Value == "barraca-iglu-2-pessoas");
        tent.Update(tent.CategoryId, "Barraca Iglu Renomeada", tent.Description, tent.Attributes);
        tent.ChangeSkuPrice(tent.Skus[0].Id, Money.Create(1.23m, "BRL").Value).IsSuccess.ShouldBeTrue();
        db.Products.Remove(products.Single(product => product.Slug.Value == RemovedSlug));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    // One line per product with its ids, name and SKU prices: an overwritten or re-created row shows up as a difference.
    private static async Task<List<string>> SnapshotAsync(WebApplicationFactory<Program> host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var products = await db.Products.AsNoTracking().Include(product => product.Skus).ToListAsync(TestContext.Current.CancellationToken);
        return [.. products
            .OrderBy(product => product.Slug.Value, StringComparer.Ordinal)
            .Select(product => $"{product.Slug.Value}|{product.Id.Value}|{product.CategoryId.Value}|{product.Name}|{Prices(product.Skus)}")];
    }

    private static string Prices(IEnumerable<Sku> skus) =>
        string.Join(',', skus.OrderBy(sku => sku.Id.Value).Select(sku => sku.Id.Value + ":" + sku.Price.Amount.ToString(CultureInfo.InvariantCulture)));
}
