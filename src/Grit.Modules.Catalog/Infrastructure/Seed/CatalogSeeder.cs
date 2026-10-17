using System.Text.Json;
using Grit.Modules.Catalog.Domain;
using Grit.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Infrastructure.Seed;

// Idempotent by slug: an existing category or product is left as it is, so a restart adds only what is missing.
// One SaveChanges for the whole file: a seed that fails half-way writes nothing.
internal sealed class CatalogSeeder(CatalogDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SeedReport> SeedAsync(string path, CancellationToken cancellationToken)
    {
        SeedCatalog? catalog;
        await using (var file = File.OpenRead(path))
        {
            catalog = await JsonSerializer.DeserializeAsync<SeedCatalog>(file, JsonOptions, cancellationToken);
        }

        if (catalog is null)
        {
            throw new InvalidOperationException($"The seed file '{path}' is empty.");
        }

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

    private async Task<CategoryId> FindOrAddCategoryAsync(string name, CancellationToken cancellationToken)
    {
        var created = Category.Create(name);
        if (created.IsFailure)
        {
            throw Invalid(name, created.Error);
        }

        var slug = created.Value.Slug;
        var existing = await db.Categories.SingleOrDefaultAsync(category => category.Slug == slug, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        db.Categories.Add(created.Value);
        return created.Value.Id;
    }

    private async Task AddProductIfMissingAsync(CategoryId categoryId, SeedProduct seed, CancellationToken cancellationToken)
    {
        var created = Product.Create(categoryId, seed.Name, seed.Description, seed.Attributes);
        if (created.IsFailure)
        {
            throw Invalid(seed.Name, created.Error);
        }

        var product = created.Value;
        if (await db.Products.AnyAsync(existing => existing.Slug == product.Slug, cancellationToken))
        {
            return;
        }

        foreach (var sku in seed.Skus)
        {
            var price = Money.Create(sku.PriceAmount, Product.StoreCurrency);
            if (price.IsFailure)
            {
                throw Invalid(seed.Name, price.Error);
            }

            var added = product.AddSku(sku.Attributes, price.Value);
            if (added.IsFailure)
            {
                throw Invalid(seed.Name, added.Error);
            }
        }

        db.Products.Add(product);
    }

    private int CountAdded<TEntity>()
        where TEntity : class =>
        db.ChangeTracker.Entries<TEntity>().Count(entry => entry.State == EntityState.Added);

    private static InvalidOperationException Invalid(string name, Error error) =>
        new($"The seed entry '{name}' is invalid: {error.Code}.");
}
