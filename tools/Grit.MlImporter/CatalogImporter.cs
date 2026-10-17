using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure.Seed;

namespace Grit.MlImporter;

// Calls are sequential; a 429 is retried by the client, any other failure ends the run.
internal sealed class CatalogImporter(MercadoLivreClient client, TextWriter log, Func<string, byte[], CancellationToken, Task>? writeFile = null)
{
    public const int MaxImagesPerProduct = 2;

    // About a quarter of catalog products has a winning listing and variants repeat a name, so a wide page is searched once.
    public const int SearchLimit = 50;

    // The seeder reads the web defaults; accents stay readable in the committed file.
    private static readonly JsonSerializerOptions SeedJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        NewLine = "\n",
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly HashSet<Slug> _productSlugs = [];

    private readonly HashSet<Slug> _triedSlugs = [];

    private readonly Dictionary<string, byte[]> _images = [];

    // The seam lets a test fail an image write; the real run writes straight to disk.
    private readonly Func<string, byte[], CancellationToken, Task> _writeFile = writeFile ?? ((path, bytes, cancellationToken) => File.WriteAllBytesAsync(path, bytes, cancellationToken));

    public async Task ImportAsync(ImportOptions options, CancellationToken cancellationToken)
    {
        var categories = new List<SeedCategory>();
        foreach (var category in options.Categories)
        {
            var products = await ImportCategoryAsync(category, options.ProductsPerCategory, cancellationToken);
            categories.Add(new SeedCategory(category.StoreName, products));
            await log.WriteLineAsync($"{category.StoreName}: {products.Count} products");
        }

        var catalog = new SeedCatalog(categories);
        CatalogSeeder.Validate(catalog);
        await WriteAsync(options.OutputDirectory, catalog, cancellationToken);
    }

    // Products without a BRL listing or already imported are skipped, so the page is wider than the wanted count.
    private async Task<List<SeedProduct>> ImportCategoryAsync(ImportCategory category, int count, CancellationToken cancellationToken)
    {
        var products = new List<SeedProduct>();
        foreach (var found in await client.SearchAsync(category.Query, category.Domain, SearchLimit, cancellationToken))
        {
            if (products.Count == count)
            {
                break;
            }

            var product = await ImportProductAsync(found, cancellationToken);
            if (product is not null)
            {
                products.Add(product);
            }
        }

        return products;
    }

    private async Task<SeedProduct?> ImportProductAsync(MlProduct found, CancellationToken cancellationToken)
    {
        var searchSlug = Slug.FromName(SeedMapper.Clean(found.Name, SeedMapper.MaxNameLength));
        if (searchSlug.IsFailure || _triedSlugs.Contains(searchSlug.Value))
        {
            await log.WriteLineAsync($"  skipped {found.Id}: no name or already tried");
            return null;
        }

        _triedSlugs.Add(searchSlug.Value);
        var id = found.Id;
        var product = await client.ProductAsync(id, cancellationToken);
        var slug = Slug.FromName(SeedMapper.Clean(product.Name, SeedMapper.MaxNameLength));
        if (slug.IsFailure || _productSlugs.Contains(slug.Value))
        {
            await log.WriteLineAsync($"  skipped {id}: no name or already imported");
            return null;
        }

        var items = await client.ItemsAsync(id, cancellationToken);
        if (SeedMapper.LowestPriceInBrl(items) is null)
        {
            await log.WriteLineAsync($"  skipped {id}: {SeedMapper.NoPrice.Message}");
            return null;
        }

        var imageFileNames = await DownloadImagesAsync(slug.Value, product.Pictures ?? [], cancellationToken);
        var mapped = SeedMapper.Map(product, items, imageFileNames);
        _productSlugs.Add(slug.Value);
        return mapped.Value;
    }

    private async Task<List<string>> DownloadImagesAsync(Slug slug, IReadOnlyList<MlPicture> pictures, CancellationToken cancellationToken)
    {
        var fileNames = new List<string>();
        foreach (var picture in pictures.Take(MaxImagesPerProduct))
        {
            var webp = WebpImage.Convert(await client.DownloadAsync(picture.Url, cancellationToken));
            if (webp is null)
            {
                await log.WriteLineAsync($"  skipped picture {picture.Url}: not an image");
                continue;
            }

            var fileName = $"{slug.Value}-{fileNames.Count + 1}.webp";
            _images[fileName] = webp;
            fileNames.Add(fileName);
        }

        return fileNames;
    }

    // A failed run leaves the previous seed intact: the new one is staged beside it and swapped in only when complete.
    private async Task WriteAsync(string outputDirectory, SeedCatalog catalog, CancellationToken cancellationToken)
    {
        var staging = Path.Combine(outputDirectory, $".import-{Guid.NewGuid():N}");
        try
        {
            await StageAsync(staging, catalog, cancellationToken);
            SeedSwap.Replace(outputDirectory, staging);
        }
        finally
        {
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }
        }

        await log.WriteLineAsync($"Wrote {Path.Combine(outputDirectory, "catalog.json")} and {_images.Count} images");
    }

    private async Task StageAsync(string stagingDirectory, SeedCatalog catalog, CancellationToken cancellationToken)
    {
        var imagesDirectory = Path.Combine(stagingDirectory, "images");
        Directory.CreateDirectory(imagesDirectory);
        foreach (var (fileName, bytes) in _images)
        {
            await _writeFile(Path.Combine(imagesDirectory, fileName), bytes, cancellationToken);
        }

        var json = JsonSerializer.Serialize(catalog, SeedJsonOptions) + "\n";
        await File.WriteAllTextAsync(Path.Combine(stagingDirectory, "catalog.json"), json, cancellationToken);
    }
}
