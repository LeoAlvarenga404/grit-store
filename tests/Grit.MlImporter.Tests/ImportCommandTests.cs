using System.Net;
using System.Text;
using System.Text.Json;
using Grit.Modules.Catalog.Infrastructure.Seed;
using SkiaSharp;

namespace Grit.MlImporter.Tests;

// The whole run against the recorded responses: search, product, items and pictures, then the files the seeder reads.
public sealed class ImportCommandTests : IDisposable
{
    private const string Api = "https://api.mercadolibre.com/";

    private const string Picture = "https://http2.mlstatic.com/D_NQ_NP_";

    private static readonly string[] Arguments = ["--category", "Camping=barraca camping", "--count", "2", "--output"];

    private readonly string _output = Path.Combine(Path.GetTempPath(), $"grit-ml-{Guid.NewGuid():N}");

    [Fact]
    public async Task RunAsync_RecordedResponses_WritesTheSeedTheSeederReads()
    {
        var handler = RecordedCatalog();
        var stale = Path.Combine(_output, "images", "stale.webp");
        Directory.CreateDirectory(Path.GetDirectoryName(stale)!);
        await File.WriteAllBytesAsync(stale, [1], TestContext.Current.CancellationToken);

        var exitCode = await RunAsync(handler, "test-token");

        exitCode.ShouldBe(ImportCommand.Succeeded);
        var catalog = await ReadSeedAsync();
        CatalogSeeder.Validate(catalog);
        var category = catalog.Categories.ShouldHaveSingleItem();
        category.Name.ShouldBe("Camping");
        category.Products.Select(product => product.Name).ShouldBe(["Barraca Camping Iglu 4 Pessoas Falcon", "Barraca Trekking Ultraleve 1 Pessoa"]);
        var tent = category.Products[0];
        tent.Description.ShouldBe("Barraca iglu para 4 pessoas com sobreteto impermeável e montagem rápida.");
        tent.Attributes.ShouldBe(new Dictionary<string, string>
        {
            ["marca"] = "Nautika",
            ["modelo"] = "Falcon",
            ["capacidade-de-pessoas"] = "4",
            ["peso"] = "3.2 kg",
        });
        var sku = tent.Skus.ShouldHaveSingleItem();
        sku.PriceAmount.ShouldBe(459.9m);
        sku.Attributes.ShouldBe(new Dictionary<string, string> { ["cor"] = "verde" });
        var group = tent.Images.ShouldNotBeNull().ShouldHaveSingleItem();
        group.Color.ShouldBe("verde");
        var images = group.Files;
        images.ShouldBe(["barraca-camping-iglu-4-pessoas-falcon-1.webp", "barraca-camping-iglu-4-pessoas-falcon-2.webp"]);
        category.Products[1].Images.ShouldBeEmpty();
        Directory.GetFiles(Path.Combine(_output, "images")).Select(Path.GetFileName).Order().ShouldBe(images);
        TestImages.Describe(await File.ReadAllBytesAsync(Path.Combine(_output, "images", images[0]), TestContext.Current.CancellationToken))
            .ShouldBe((SKEncodedImageFormat.Webp, 800, 500));
    }

    [Fact]
    public async Task RunAsync_RecordedResponses_SendsTheTokenToTheApiOnly()
    {
        var handler = RecordedCatalog();

        await RunAsync(handler, "test-token");

        handler.Requests.Where(request => request.Url.StartsWith(Api, StringComparison.Ordinal))
            .ShouldAllBe(request => request.Authorization == "Bearer test-token");
        handler.Requests.Where(request => request.Url.StartsWith(Picture, StringComparison.Ordinal))
            .ShouldAllBe(request => request.Authorization == null);
        handler.Requests.Count(request => request.Url.StartsWith(Picture, StringComparison.Ordinal)).ShouldBe(3);
    }

    [Fact]
    public async Task RunAsync_NoToken_Returns2WithTheUserSecretsCommandAndCallsNothing()
    {
        var handler = new FixtureHandler();
        var error = new StringWriter();

        var exitCode = await ImportCommand.RunAsync([.. Arguments, _output], " ", handler, TextWriter.Null, error, retryDelay: TimeSpan.Zero);

        exitCode.ShouldBe(ImportCommand.MissingToken);
        error.ToString().ShouldContain("dotnet user-secrets set \"MercadoLivre:AccessToken\"");
        handler.Requests.ShouldBeEmpty();
        Directory.Exists(_output).ShouldBeFalse();
    }

    [Fact]
    public async Task RunAsync_BadArguments_Returns1WithTheUsage()
    {
        var error = new StringWriter();

        var exitCode = await ImportCommand.RunAsync(["--count", "3"], "test-token", new FixtureHandler(), TextWriter.Null, error, retryDelay: TimeSpan.Zero);

        exitCode.ShouldBe(ImportCommand.InvalidArguments);
        error.ToString().ShouldContain("Usage: dotnet run --project tools/Grit.MlImporter");
    }

    [Fact]
    public async Task RunAsync_TokenRejected_Returns3AndWritesNothing()
    {
        var handler = new FixtureHandler().Json(SearchUrl, "items-not-found.json", HttpStatusCode.Unauthorized);
        var error = new StringWriter();

        var exitCode = await ImportCommand.RunAsync([.. Arguments, _output], "expired-token", handler, TextWriter.Null, error, retryDelay: TimeSpan.Zero);

        exitCode.ShouldBe(ImportCommand.RequestFailed);
        error.ToString().ShouldContain("401");
        error.ToString().ShouldContain("/products/search?");
        Directory.Exists(_output).ShouldBeFalse();
    }

    [Fact]
    public async Task RunAsync_ThrottledTwice_RetriesAndSucceeds()
    {
        var handler = RecordedCatalog().Throttled(SearchUrl, 2);

        var exitCode = await RunAsync(handler, "test-token");

        exitCode.ShouldBe(ImportCommand.Succeeded);
        handler.Requests.Count(request => request.Url == SearchUrl).ShouldBe(3);
    }

    [Fact]
    public async Task RunAsync_ThrottledEveryTime_Returns3NamingTheUrl()
    {
        var handler = RecordedCatalog().Throttled(SearchUrl, 10);
        var error = new StringWriter();

        var exitCode = await ImportCommand.RunAsync([.. Arguments, _output], "test-token", handler, TextWriter.Null, error, retryDelay: TimeSpan.Zero);

        exitCode.ShouldBe(ImportCommand.RequestFailed);
        handler.Requests.Count(request => request.Url == SearchUrl).ShouldBe(MercadoLivreClient.MaxAttempts);
        error.ToString().ShouldContain("429");
        error.ToString().ShouldContain("/products/search?");
        Directory.Exists(_output).ShouldBeFalse();
    }

    [Fact]
    public async Task RunAsync_RepeatedNameInTheSearch_SkipsItWithoutFetchingIt()
    {
        var handler = RecordedCatalog();

        await RunAsync(handler, "test-token");

        handler.Requests.Select(request => request.Url).ShouldNotContain($"{Api}products/MLB1003");
    }

    [Fact]
    public async Task RunAsync_Domain_SearchesInsideIt()
    {
        var handler = RecordedCatalog().Json($"{SearchUrl}&domain_id=MLB-TENTS", "search-barraca.json");
        string[] arguments = ["--category", "Camping=barraca camping@MLB-TENTS", "--count", "2", "--output", _output];

        var exitCode = await ImportCommand.RunAsync(arguments, "test-token", handler, TextWriter.Null, TextWriter.Null, retryDelay: TimeSpan.Zero);

        exitCode.ShouldBe(ImportCommand.Succeeded);
        handler.Requests.Select(request => request.Url).ShouldContain($"{SearchUrl}&domain_id=MLB-TENTS");
    }

    [Fact]
    public async Task ImportAsync_ImageWriteFails_KeepsThePreviousSeed()
    {
        Directory.CreateDirectory(Path.Combine(_output, "images"));
        await File.WriteAllTextAsync(Path.Combine(_output, "catalog.json"), "old", TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(_output, "images", "old.webp"), [1], TestContext.Current.CancellationToken);
        var options = ImportOptions.Parse([.. Arguments, _output]).Value;
        using var http = new HttpClient(RecordedCatalog(), disposeHandler: false);
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

    private static string SearchUrl => $"{Api}products/search?status=active&site_id=MLB&q=barraca%20camping&limit=50&offset=0";

    // MLB1002 has no listing (404), MLB1003 repeats MLB1001's name, and MLB1004's picture is an error page.
    private static FixtureHandler RecordedCatalog() => new FixtureHandler()
        .Json(SearchUrl, "search-barraca.json")
        .Json($"{Api}products/MLB1001", "product-MLB1001.json")
        .Json($"{Api}products/MLB1001/items", "items-MLB1001.json")
        .Json($"{Api}products/MLB1002", "product-MLB1002.json")
        .Json($"{Api}products/MLB1002/items", "items-not-found.json", HttpStatusCode.NotFound)
        .Json($"{Api}products/MLB1004", "product-MLB1004.json")
        .Json($"{Api}products/MLB1004/items", "items-MLB1004.json")
        .Bytes($"{Picture}612345-MLA1001-F.jpg", TestImages.Jpeg(1200, 750))
        .Bytes($"{Picture}612346-MLA1001-F.jpg", TestImages.Jpeg(1200, 750))
        .Bytes($"{Picture}800001-MLA1004-F.jpg", Encoding.UTF8.GetBytes("<html>Erro 503</html>"));

    private Task<int> RunAsync(FixtureHandler handler, string token) =>
        ImportCommand.RunAsync([.. Arguments, _output], token, handler, TextWriter.Null, TextWriter.Null, retryDelay: TimeSpan.Zero);

    private async Task<SeedCatalog> ReadSeedAsync()
    {
        await using var file = File.OpenRead(Path.Combine(_output, "catalog.json"));
        return (await JsonSerializer.DeserializeAsync<SeedCatalog>(file, JsonSerializerOptions.Web, TestContext.Current.CancellationToken))!;
    }
}
