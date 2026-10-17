using Grit.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Catalog.Infrastructure.Seed;

// Development load for k6 (Seed:VolumeProducts). Each batch is one SaveChanges, so one transaction, and inserts only
// the slugs not stored yet: a restart, a crash or a larger count adds only what is missing.
internal sealed class VolumeSeeder(CatalogDbContext db)
{
    public const int BatchSize = 1000;

    public async Task<int> SeedAsync(int count, CancellationToken cancellationToken)
    {
        var catalog = new VolumeCatalog(await FindOrAddCategoriesAsync(cancellationToken));
        var added = 0;
        for (var start = 0; start < count; start += BatchSize)
        {
            var batch = Enumerable.Range(start, Math.Min(BatchSize, count - start)).Select(catalog.Generate).ToList();
            var missing = await MissingAsync(batch, cancellationToken);
            if (missing.Count == 0)
            {
                continue;
            }

            db.Products.AddRange(missing);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
            added += missing.Count;
        }

        return added;
    }

    private async Task<List<Product>> MissingAsync(List<Product> batch, CancellationToken cancellationToken)
    {
        var slugs = batch.Select(product => product.Slug.Value).ToArray();
        var stored = (await db.Database
            .SqlQuery<string>($"SELECT slug AS \"Value\" FROM catalog.products WHERE slug = ANY({slugs})")
            .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        return batch.Where(product => !stored.Contains(product.Slug.Value)).ToList();
    }

    private async Task<Dictionary<string, CategoryId>> FindOrAddCategoriesAsync(CancellationToken cancellationToken)
    {
        var categoryIds = new Dictionary<string, CategoryId>();
        foreach (var name in VolumeCatalog.CategoryNames)
        {
            var created = Category.Create(name).Value;
            var existing = await db.Categories.SingleOrDefaultAsync(category => category.Slug == created.Slug, cancellationToken);
            if (existing is null)
            {
                db.Categories.Add(created);
            }

            categoryIds[name] = existing?.Id ?? created.Id;
        }

        await db.SaveChangesAsync(cancellationToken);
        return categoryIds;
    }
}
