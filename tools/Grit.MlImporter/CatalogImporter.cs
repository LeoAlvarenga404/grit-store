using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure.Seed;

namespace Grit.MlImporter;

// Calls are sequential; the client retries 429, 500, 502, 503 and 504 (5 attempts, 5/10/15/20 s apart) and skips a dead picture; any other failure ends the run.
internal sealed class CatalogImporter(MercadoLivreClient client, TextWriter log, Func<string, byte[], CancellationToken, Task>? writeFile = null, SeedCatalog? ownLine = null)
{
    // 3 photos x ~3 colours x 10 models x 2 imported categories x ~30 KB: about 6 MB of committed images.
    public const int MaxImagesPerColor = 3;

    // ponytail: at most 10 search pages per source, then the next source; widen it if a category comes out short.
    public const int MaxSearchOffset = 500;

    // The seeder reads the web defaults; accents stay readable in the committed file.
    private static readonly JsonSerializerOptions SeedJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        NewLine = "\n",
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly FamilyReader _families = new(client, log);

    // An own-line product name is taken from the start, so a model with the same slug is skipped like a repeat.
    private readonly HashSet<Slug> _productSlugs = [.. (ownLine?.Categories ?? []).SelectMany(category => category.Products).Select(product => Slug.FromName(product.Name).Value)];

    private readonly SeedCatalog _ownLine = ownLine ?? new SeedCatalog([]);

    private readonly Dictionary<string, byte[]> _images = [];

    // The seam lets a test fail an image write; the real run writes straight to disk.
    private readonly Func<string, byte[], CancellationToken, Task> _writeFile = writeFile ?? ((path, bytes, cancellationToken) => File.WriteAllBytesAsync(path, bytes, cancellationToken));

    // A store category repeated on the command line is one category searched with each source in turn.
    public async Task ImportAsync(ImportOptions options, CancellationToken cancellationToken)
    {
        var categories = new List<SeedCategory>();
        foreach (var sources in options.Categories.GroupBy(category => category.StoreName))
        {
            var products = new List<SeedProduct>();
            foreach (var source in sources)
            {
                await ImportSourceAsync(source, products, options.ProductsPerCategory, cancellationToken);
            }

            categories.Add(new SeedCategory(sources.Key, products));
            await log.WriteLineAsync($"{sources.Key}: {products.Count} products");
        }

        var catalog = new SeedCatalog(categories);
        // The seeder validates both files as one catalogue; checking the same union here keeps a bad seed from being written.
        CatalogSeeder.Validate(new SeedCatalog([.. catalog.Categories, .. _ownLine.Categories]));
        await WriteAsync(options.OutputDirectory, catalog, cancellationToken);
    }

    // Pages until the category has its models, the results end or the offset cap is reached.
    private async Task ImportSourceAsync(ImportCategory source, List<SeedProduct> products, int count, CancellationToken cancellationToken)
    {
        for (var offset = 0; products.Count < count && offset < MaxSearchOffset; offset += MercadoLivreClient.SearchLimit)
        {
            var page = await client.SearchAsync(source.Query, source.Domain, offset, cancellationToken);
            foreach (var hit in page.Results.TakeWhile(_ => products.Count < count))
            {
                await ImportHitAsync(hit.Id, products, count, cancellationToken);
            }

            if (offset + MercadoLivreClient.SearchLimit >= (page.Paging?.Total ?? 0))
            {
                return;
            }
        }
    }

    // Pickers can lead to another model's variants: each parent is one store product, and leaves without a parent
    // belong to the hit's model.
    private async Task ImportHitAsync(string hitId, List<SeedProduct> products, int count, CancellationToken cancellationToken)
    {
        if (_families.HasSeen(hitId))
        {
            await log.WriteLineAsync($"  skipped {hitId}: part of a family already read");
            return;
        }

        var leaves = await _families.ReadLeavesAsync(hitId, cancellationToken);
        foreach (var model in leaves.GroupBy(leaf => leaf.ParentId ?? hitId).TakeWhile(_ => products.Count < count))
        {
            var product = await ImportModelAsync(model.Key, [.. model], cancellationToken);
            if (product is not null)
            {
                products.Add(product);
            }
        }
    }

    private async Task<SeedProduct?> ImportModelAsync(string modelId, List<MlProduct> leaves, CancellationToken cancellationToken)
    {
        // Most leaves have no offer: a model with fewer than 2 colours is dropped before its listings are read.
        if (leaves.Select(ModelMapper.ColorOf).OfType<string>().Distinct().Count() < ModelMapper.MinColors)
        {
            await log.WriteLineAsync($"  skipped {modelId}: shows fewer than 2 colours");
            return null;
        }

        var priced = await PriceAsync(leaves, cancellationToken);
        var mapped = ModelMapper.Map(priced);
        if (mapped.IsFailure)
        {
            await log.WriteLineAsync($"  skipped {modelId}: {mapped.Error.Message}");
            return null;
        }

        var slug = Slug.FromName(mapped.Value.Name).Value;
        if (!_productSlugs.Add(slug))
        {
            await log.WriteLineAsync($"  skipped {modelId}: {slug.Value} already imported or in the own line");
            return null;
        }

        var images = await DownloadImagesAsync(slug, ModelMapper.PicturesByColor(priced, MaxImagesPerColor), cancellationToken);
        return mapped.Value with { Images = images };
    }

    // A leaf without a colour is never sold, so it is not priced; the buy box saves the listings call when it has a BRL price.
    private async Task<List<PricedLeaf>> PriceAsync(List<MlProduct> leaves, CancellationToken cancellationToken)
    {
        var priced = new List<PricedLeaf>();
        foreach (var leaf in leaves.Where(leaf => ModelMapper.ColorOf(leaf) is not null))
        {
            var price = SeedMapper.PriceOf(leaf.BuyBoxWinner) ?? SeedMapper.LowestPriceInBrl(await client.ItemsAsync(leaf.Id, cancellationToken));
            if (price is null)
            {
                await log.WriteLineAsync($"  skipped {leaf.Id}: {SeedMapper.NoPrice.Message}");
                continue;
            }

            priced.Add(new PricedLeaf(leaf, price.Value));
        }

        return priced;
    }

    // "{slug}-{colour number}-{photo number}.webp", both 1-based, numbering only colours that got pictures; a colour whose pictures all fail is left without images.
    private async Task<List<SeedColorImages>> DownloadImagesAsync(Slug slug, IReadOnlyList<ColorPictures> colors, CancellationToken cancellationToken)
    {
        var groups = new List<SeedColorImages>();
        foreach (var color in colors)
        {
            var files = new List<string>();
            foreach (var url in color.Urls)
            {
                var webp = WebpImage.Convert(await client.DownloadAsync(url, cancellationToken));
                if (webp is null)
                {
                    await log.WriteLineAsync($"  skipped picture {url}: not an image");
                    continue;
                }

                var fileName = $"{slug.Value}-{groups.Count + 1}-{files.Count + 1}.webp";
                _images[fileName] = webp;
                files.Add(fileName);
            }

            if (files.Count > 0)
            {
                groups.Add(new SeedColorImages(color.Color, files));
            }
        }

        return groups;
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
