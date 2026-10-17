using System.Text.Json;
using Grit.Modules.Catalog.Domain;
using Grit.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Infrastructure.Seed;

// Idempotent by slug: an existing category or product is left as it is, so a restart adds only what is missing.
// The whole file is checked before the database is read, so a bad file fails on an empty and on a seeded catalog alike;
// one SaveChanges for the whole file, so nothing is written unless everything is.
internal sealed class CatalogSeeder(CatalogDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SeedReport> SeedAsync(string path, CancellationToken cancellationToken)
    {
        var catalog = await ReadAsync(path, cancellationToken);
        Validate(catalog);

        foreach (var seedCategory in catalog.Categories)
        {
            var categoryId = await FindOrAddCategoryAsync(seedCategory.Name, cancellationToken);
            foreach (var seedProduct in seedCategory.Products)
            {
                await AddProductIfMissingAsync(categoryId, seedProduct, cancellationToken);
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
    private static void Validate(SeedCatalog catalog)
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

    private async Task AddProductIfMissingAsync(CategoryId categoryId, SeedProduct seed, CancellationToken cancellationToken)
    {
        var product = Build(categoryId, seed);
        if (await db.Products.AnyAsync(existing => existing.Slug == product.Slug, cancellationToken))
        {
            return;
        }

        db.Products.Add(product);
    }

    private static Product Build(CategoryId categoryId, SeedProduct seed)
    {
        var product = Valid(seed.Name, Product.Create(categoryId, seed.Name, seed.Description, seed.Attributes));
        foreach (var sku in seed.Skus)
        {
            var price = Valid(seed.Name, Money.Create(sku.PriceAmount, Product.StoreCurrency));
            Valid(seed.Name, product.AddSku(sku.Attributes, price));
        }

        return product;
    }

    private static T Valid<T>(string name, Result<T> result) =>
        result.IsSuccess ? result.Value : throw new InvalidOperationException($"The seed entry '{name}' is invalid: {result.Error.Code}.");

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
