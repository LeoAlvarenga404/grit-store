using System.Text.Json;
using Grit.Modules.Catalog.Infrastructure.Seed;
using SkiaSharp;

namespace Grit.MlImporter.Tests;

// The whole run against the recorded families: search pages, family walk, prices, pictures, then the files the seeder reads.
public sealed class CatalogImporterTests : IDisposable
{
    private readonly string _output = Path.Combine(Path.GetTempPath(), $"grit-ml-{Guid.NewGuid():N}");

    [Fact]
    public async Task ImportAsync_RecordedFamilies_WritesOneProductPerModelWithItsObservedVariants()
    {
        await ImportAsync(RecordedCatalog.Handler(), "--category", RecordedCatalog.Category, "--count", "2");

        var catalog = await ReadSeedAsync();
        CatalogSeeder.Validate(catalog);
        var category = catalog.Categories.ShouldHaveSingleItem();
        category.Name.ShouldBe("Camisetas e regatas");
        category.Products.Select(product => product.Name).ShouldBe(["Camiseta Dry Fit Grit", "Camiseta Térmica Grit"]);
        var shirt = category.Products[0];
        shirt.Description.ShouldBe("Camiseta leve que seca rápido.");
        shirt.Attributes.ShouldBe(new Dictionary<string, string> { ["marca"] = "Grit", ["genero"] = "Unissex" });
        shirt.Skus.Select(sku => (sku.Attributes["cor"], sku.Attributes["tamanho"], sku.PriceAmount))
            .ShouldBe([("preto", "M", 79.9m), ("preto", "G", 84.9m), ("azul", "G", 89.9m)]);
    }

    [Fact]
    public async Task ImportAsync_RecordedFamilies_GroupsUpToThreeDistinctPicturesPerColor()
    {
        await ImportAsync(RecordedCatalog.Handler(), "--category", RecordedCatalog.Category, "--count", "2");

        var catalog = await ReadSeedAsync();
        var shirt = catalog.Categories[0].Products[0];
        var images = shirt.Images.ShouldNotBeNull();
        images.Select(group => group.Color).ShouldBe(["preto", "azul"]);
        images[0].Files.ShouldBe(["camiseta-dry-fit-grit-1-1.webp", "camiseta-dry-fit-grit-1-2.webp", "camiseta-dry-fit-grit-1-3.webp"]);
        images[1].Files.ShouldBe(["camiseta-dry-fit-grit-2-1.webp"]);
        var thermal = catalog.Categories[0].Products[1];
        var thermalImages = thermal.Images.ShouldNotBeNull().ShouldHaveSingleItem();
        thermalImages.Color.ShouldBe("verde");
        thermalImages.Files.ShouldBe(["camiseta-termica-grit-1-1.webp"]);
        Directory.GetFiles(Path.Combine(_output, "images")).Select(Path.GetFileName).Order(StringComparer.Ordinal).ShouldBe(
        [
            "camiseta-dry-fit-grit-1-1.webp", "camiseta-dry-fit-grit-1-2.webp", "camiseta-dry-fit-grit-1-3.webp",
            "camiseta-dry-fit-grit-2-1.webp", "camiseta-termica-grit-1-1.webp",
        ]);
        TestImages.Describe(await File.ReadAllBytesAsync(Path.Combine(_output, "images", "camiseta-dry-fit-grit-1-1.webp"), TestContext.Current.CancellationToken))
            .ShouldBe((SKEncodedImageFormat.Webp, 800, 500));
        await ShouldShowAsync("camiseta-dry-fit-grit-1-1.webp", "700001-MLA2001");
        await ShouldShowAsync("camiseta-dry-fit-grit-1-2.webp", "700002-MLA2001");
        await ShouldShowAsync("camiseta-dry-fit-grit-1-3.webp", "700003-MLA2002");
        await ShouldShowAsync("camiseta-dry-fit-grit-2-1.webp", "700005-MLA2004");
        await ShouldShowAsync("camiseta-termica-grit-1-1.webp", "710002-MLA2021");
    }

    [Fact]
    public async Task ImportAsync_PickerIntoAnotherParent_ImportsEachParentAsItsOwnProduct()
    {
        await ImportAsync(RecordedCatalog.Handler(), "--category", RecordedCatalog.SneakersCategory, "--count", "5");

        var products = (await ReadSeedAsync()).Categories.ShouldHaveSingleItem().Products;
        products.Select(product => product.Name).ShouldBe(["Tênis Veloz Grit", "Tênis Veloz Grit Couro"]);
        products[0].Skus.Select(sku => (sku.Attributes["cor"], sku.Attributes["tamanho"], sku.PriceAmount)).ShouldBe([("preto", "40 BR", 199.9m), ("branco", "40 BR", 209.9m)]);
        products[1].Skus.Select(sku => (sku.Attributes["cor"], sku.Attributes["tamanho"], sku.PriceAmount)).ShouldBe([("preto", "40 BR", 149.9m), ("branco", "40 BR", 159.9m)]);
        products[0].Images.ShouldNotBeNull().SelectMany(group => group.Files).ShouldBe(["tenis-veloz-grit-1-1.webp", "tenis-veloz-grit-2-1.webp"]);
        products[1].Images.ShouldNotBeNull().SelectMany(group => group.Files).ShouldBe(["tenis-veloz-grit-couro-1-1.webp", "tenis-veloz-grit-couro-2-1.webp"]);
        await ShouldShowAsync("tenis-veloz-grit-1-1.webp", "720031-MLA2031");
        await ShouldShowAsync("tenis-veloz-grit-2-1.webp", "720032-MLA2032");
        await ShouldShowAsync("tenis-veloz-grit-couro-1-1.webp", "720041-MLA2041");
        await ShouldShowAsync("tenis-veloz-grit-couro-2-1.webp", "720042-MLA2042");
    }

    [Fact]
    public async Task ImportAsync_HitInsideAFamilyAlreadyRead_IsNotReadAgain()
    {
        var handler = RecordedCatalog.Handler();

        await ImportAsync(handler, "--category", RecordedCatalog.Category, "--count", "2");

        handler.Requests.Count(request => request.Url == $"{RecordedCatalog.Api}products/MLB2002").ShouldBe(1);
    }

    [Fact]
    public async Task ImportAsync_SingleColorModel_IsSkippedWithoutDownloadingItsPictures()
    {
        var handler = RecordedCatalog.Handler();

        await ImportAsync(handler, "--category", RecordedCatalog.Category, "--count", "2");

        (await ReadSeedAsync()).Categories[0].Products.Select(product => product.Name).ShouldNotContain("Regata Básica Grit");
        handler.Requests.Select(request => request.Url).ShouldNotContain($"{RecordedCatalog.Picture}700010-MLA2010-F.jpg");
        handler.Requests.Select(request => request.Url).ShouldNotContain($"{RecordedCatalog.Api}products/MLB2010/items");
    }

    [Fact]
    public async Task ImportAsync_CountReachedOnTheFirstPage_ReadsNoOtherPage()
    {
        var handler = RecordedCatalog.Handler();

        await ImportAsync(handler, "--category", RecordedCatalog.Category, "--count", "1");

        var urls = handler.Requests.Select(request => request.Url).ToList();
        urls.ShouldNotContain(RecordedCatalog.SecondPage);
        urls.ShouldNotContain($"{RecordedCatalog.Api}products/MLB2010");
        (await ReadSeedAsync()).Categories[0].Products.ShouldHaveSingleItem().Name.ShouldBe("Camiseta Dry Fit Grit");
    }

    [Fact]
    public async Task ImportAsync_SameStoreCategoryTwice_SearchesBothIntoOneCategory()
    {
        var handler = RecordedCatalog.Handler();

        await ImportAsync(handler, "--category", RecordedCatalog.Category, "--category", "Camisetas e regatas=regata@MLB-TANK_TOPS", "--count", "3");

        var category = (await ReadSeedAsync()).Categories.ShouldHaveSingleItem();
        category.Products.Select(product => product.Name).ShouldBe(["Camiseta Dry Fit Grit", "Camiseta Térmica Grit", "Regata Leve Grit"]);
        var regata = category.Products[2];
        regata.Skus.Select(sku => (sku.Attributes["cor"], sku.Attributes["tamanho"], sku.PriceAmount)).ShouldBe([("amarelo", "M", 59.9m), ("roxo", "M", 64.9m)]);
        regata.Images.ShouldNotBeNull().SelectMany(group => group.Files).ShouldBe(["regata-leve-grit-1-1.webp", "regata-leve-grit-2-1.webp"]);
        await ShouldShowAsync("regata-leve-grit-1-1.webp", "730060-MLA2060");
        await ShouldShowAsync("regata-leve-grit-2-1.webp", "730061-MLA2061");
    }

    // The store's own line lives in grit/ next to the imported seed (CatalogSeeder.OwnLineFolder): a run never touches it.
    [Fact]
    public async Task ImportAsync_OwnLineFolder_IsLeftUntouched()
    {
        var ownLine = Path.Combine(_output, "grit", "catalog.json");
        Directory.CreateDirectory(Path.GetDirectoryName(ownLine)!);
        await File.WriteAllTextAsync(ownLine, "own line", TestContext.Current.CancellationToken);

        await ImportAsync(RecordedCatalog.Handler(), "--category", RecordedCatalog.Category, "--count", "2");

        (await File.ReadAllTextAsync(ownLine, TestContext.Current.CancellationToken)).ShouldBe("own line");
    }

    [Fact]
    public async Task ImportAsync_ImageWriteFails_KeepsThePreviousSeed()
    {
        Directory.CreateDirectory(Path.Combine(_output, "images"));
        await File.WriteAllTextAsync(Path.Combine(_output, "catalog.json"), "old", TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(_output, "images", "old.webp"), [1], TestContext.Current.CancellationToken);
        var options = ImportOptions.Parse(["--category", RecordedCatalog.Category, "--count", "2", "--output", _output]).Value;
        using var http = new HttpClient(RecordedCatalog.Handler(), disposeHandler: false);
        var importer = new CatalogImporter(new MercadoLivreClient(http, "test-token", TimeSpan.Zero), TextWriter.Null, (_, _, _) => throw new IOException("No space left on device."));

        await Should.ThrowAsync<IOException>(() => importer.ImportAsync(options, TestContext.Current.CancellationToken));

        (await File.ReadAllTextAsync(Path.Combine(_output, "catalog.json"), TestContext.Current.CancellationToken)).ShouldBe("old");
        File.Exists(Path.Combine(_output, "images", "old.webp")).ShouldBeTrue();
        Directory.GetFileSystemEntries(_output).Select(Path.GetFileName).Order().ShouldBe(["catalog.json", "images"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_output))
        {
            Directory.Delete(_output, recursive: true);
        }
    }

    private async Task ImportAsync(FixtureHandler handler, params string[] arguments)
    {
        var options = ImportOptions.Parse([.. arguments, "--output", _output]).Value;
        using var http = new HttpClient(handler, disposeHandler: false);
        await new CatalogImporter(new MercadoLivreClient(http, "test-token", TimeSpan.Zero), TextWriter.Null).ImportAsync(options, TestContext.Current.CancellationToken);
    }

    private async Task ShouldShowAsync(string fileName, string picture)
    {
        var bytes = await File.ReadAllBytesAsync(Path.Combine(_output, "images", fileName), TestContext.Current.CancellationToken);
        TestImages.IsNear(TestImages.CenterColor(bytes), RecordedCatalog.Markers[picture]).ShouldBeTrue($"{fileName} should show picture {picture}");
    }

    private async Task<SeedCatalog> ReadSeedAsync()
    {
        await using var file = File.OpenRead(Path.Combine(_output, "catalog.json"));
        return (await JsonSerializer.DeserializeAsync<SeedCatalog>(file, JsonSerializerOptions.Web, TestContext.Current.CancellationToken))!;
    }
}
