using System.Text.Json;
using Grit.Modules.Catalog.Domain;
using Grit.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Infrastructure.Seed;

// Idempotent by slug: an existing category or product is left as it is, so a restart adds only what is missing.
// The whole file is checked before the database is read, so a bad file fails on an empty and on a seeded catalog alike;
// one SaveChanges for the whole file, so nothing is written unless everything is (images uploaded before a failed save
// stay in the storage as orphans, like an unconfirmed upload: ADR 0016).
internal sealed class CatalogSeeder(CatalogDbContext db, ProductImageStorage images)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SeedReport> SeedAsync(string path, CancellationToken cancellationToken)
    {
        var catalog = await ReadAsync(path, cancellationToken);
        Validate(catalog);
        var imagesDirectory = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, "images");

        foreach (var seedCategory in catalog.Categories)
        {
            var categoryId = await FindOrAddCategoryAsync(seedCategory.Name, cancellationToken);
            foreach (var seedProduct in seedCategory.Products)
            {
                await AddProductIfMissingAsync(categoryId, seedProduct, imagesDirectory, cancellationToken);
            }
        }

        var report = new SeedReport(CountAdded<Category>(), CountAdded<Product>());
        await db.SaveChangesAsync(cancellationToken);
        return report;
    }

    private static async Task<SeedCatalog> ReadAsync(string path, CancellationToken cancellationToken)
    {
        await using var file = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SeedCatalog>(file, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"The seed file '{path}' is empty.");
    }

    // A slug repeated inside the file would otherwise be skipped silently once its first entry is stored.
    // Internal for tools/Grit.MlImporter, which checks its output with the same rules before writing it.
    internal static void Validate(SeedCatalog catalog)
    {
        var categorySlugs = new HashSet<Slug>();
        var productSlugs = new HashSet<Slug>();
        foreach (var seedCategory in catalog.Categories)
        {
            var category = Valid(seedCategory.Name, Category.Create(seedCategory.Name));
            Unique(categorySlugs, category.Slug, seedCategory.Name);
            foreach (var seedProduct in seedCategory.Products)
            {
                Unique(productSlugs, Build(category.Id, seedProduct).Slug, seedProduct.Name);
            }
        }
    }

    private async Task<CategoryId> FindOrAddCategoryAsync(string name, CancellationToken cancellationToken)
    {
        var created = Category.Create(name).Value;
        var existing = await db.Categories.SingleOrDefaultAsync(category => category.Slug == created.Slug, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        db.Categories.Add(created);
        return created.Id;
    }

    private async Task AddProductIfMissingAsync(CategoryId categoryId, SeedProduct seed, string imagesDirectory, CancellationToken cancellationToken)
    {
        var product = Build(categoryId, seed);
        if (await db.Products.AnyAsync(existing => existing.Slug == product.Slug, cancellationToken))
        {
            return;
        }

        await AttachImagesAsync(product, seed, imagesDirectory, cancellationToken);
        db.Products.Add(product);
    }

    // Optional: without the storage (most tests) or the files (a hand-written seed, the compose image) a product has no images.
    private async Task AttachImagesAsync(Product product, SeedProduct seed, string imagesDirectory, CancellationToken cancellationToken)
    {
        if (!images.IsConfigured)
        {
            return;
        }

        foreach (var fileName in seed.Images ?? [])
        {
            var file = Path.Combine(imagesDirectory, fileName);
            if (!File.Exists(file))
            {
                continue;
            }

            var key = Product.ImageKeyPrefix(product.Id) + fileName;
            await images.PutFileAsync(key, file, "image/webp", cancellationToken);
            Valid(seed.Name, product.AddImage(key, product.Skus is [var first, ..] ? first.Color : null));
        }
    }

    private static Product Build(CategoryId categoryId, SeedProduct seed)
    {
        var product = Valid(seed.Name, Product.Create(categoryId, seed.Name, seed.Description, seed.Attributes));
        CheckImages(seed);
        foreach (var sku in seed.Skus)
        {
            var price = Valid(seed.Name, Money.Create(sku.PriceAmount, Product.StoreCurrency));
            Valid(seed.Name, product.AddSku(sku.Attributes, price));
        }

        return product;
    }

    private static void CheckImages(SeedProduct seed)
    {
        var fileNames = seed.Images ?? [];
        if (fileNames.Count > Product.MaxImagesPerColor || !fileNames.All(IsWebpFileName))
        {
            throw new InvalidOperationException($"The seed entry '{seed.Name}' must list at most {Product.MaxImagesPerColor} .webp file names.");
        }
    }

    // A bare file name: the seed must not reach outside its images folder.
    private static bool IsWebpFileName(string image) =>
        image.Length > ".webp".Length && image.EndsWith(".webp", StringComparison.Ordinal) && Path.GetFileName(image) == image;

    private static T Valid<T>(string name, Result<T> result) =>
        result.IsSuccess ? result.Value : throw new InvalidOperationException($"The seed entry '{name}' is invalid: {result.Error.Code}.");

    private static void Valid(string name, Result result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"The seed entry '{name}' is invalid: {result.Error.Code}.");
        }
    }

    private static void Unique(HashSet<Slug> slugs, Slug slug, string name)
    {
        if (!slugs.Add(slug))
        {
            throw new InvalidOperationException($"The seed entry '{name}' repeats the slug '{slug.Value}'.");
        }
    }

    private int CountAdded<TEntity>()
        where TEntity : class =>
        db.ChangeTracker.Entries<TEntity>().Count(entry => entry.State == EntityState.Added);
}
