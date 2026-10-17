using System.Net;
using System.Text.Json;
using Grit.Modules.Catalog.Infrastructure.Seed;

namespace Grit.MlImporter.Tests;

// Runs that stop early or skip something: the count reached inside one family, a repeated product name, a missing picture.
public sealed class CatalogImporterLimitTests : IDisposable
{
    private readonly string _output = Path.Combine(Path.GetTempPath(), $"grit-ml-{Guid.NewGuid():N}");

    [Fact]
    public async Task ImportAsync_CountReachedInsideOneFamily_StopsThere()
    {
        var handler = RecordedCatalog.Handler();

        await ImportAsync(handler, "--category", RecordedCatalog.SneakersCategory, "--count", "1");

        (await ReadSeedAsync()).Categories.ShouldHaveSingleItem().Products.ShouldHaveSingleItem().Name.ShouldBe("Tênis Veloz Grit");
        var urls = handler.Requests.Select(request => request.Url).ToList();
        urls.ShouldNotContain($"{RecordedCatalog.Picture}720041-MLA2041-F.jpg");
        urls.ShouldNotContain($"{RecordedCatalog.Picture}720042-MLA2042-F.jpg");
    }

    [Fact]
    public async Task ImportAsync_SecondModelWithTheSameName_IsSkippedBeforeItsPictures()
    {
        var handler = RecordedCatalog.Handler();

        await ImportAsync(handler, "--category", RecordedCatalog.TwinsCategory, "--count", "5");

        (await ReadSeedAsync()).Categories.ShouldHaveSingleItem().Products.Select(product => product.Name)
            .ShouldBe(["Tênis Veloz Grit", "Tênis Veloz Grit Couro"]);
        handler.Requests.Select(request => request.Url).ShouldNotContain($"{RecordedCatalog.Picture}720051-MLA2051-F.jpg");
    }

    [Fact]
    public async Task ImportAsync_PictureNotFound_SkipsItAndKeepsTheRun()
    {
        var handler = RecordedCatalog.Handler().Json($"{RecordedCatalog.Picture}710002-MLA2021-F.jpg", "items-not-found.json", HttpStatusCode.NotFound);

        await ImportAsync(handler, "--category", RecordedCatalog.Category, "--count", "2");

        var products = (await ReadSeedAsync()).Categories.ShouldHaveSingleItem().Products;
        products.Select(product => product.Name).ShouldBe(["Camiseta Dry Fit Grit", "Camiseta Térmica Grit"]);
        products[1].Images.ShouldNotBeNull().ShouldBeEmpty();
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

    private async Task<SeedCatalog> ReadSeedAsync()
    {
        await using var file = File.OpenRead(Path.Combine(_output, "catalog.json"));
        return (await JsonSerializer.DeserializeAsync<SeedCatalog>(file, JsonSerializerOptions.Web, TestContext.Current.CancellationToken))!;
    }
}
