using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure.Seed;

namespace Grit.MlImporter;

// The store's own line (grit/catalog.json next to the seed) is seeded with the import as one catalogue, so an imported
// category or product with one of its slugs would stop the API at start-up.
internal static class OwnLine
{
    public static async Task<SeedCatalog> ReadAsync(string outputDirectory, CancellationToken cancellationToken)
    {
        var path = Path.Combine(outputDirectory, CatalogSeeder.OwnLineFolder, "catalog.json");
        return File.Exists(path) ? await CatalogSeeder.ReadAsync(path, cancellationToken) : new SeedCatalog([]);
    }

    public static string? CategoryTakenBy(ImportOptions options, SeedCatalog ownLine)
    {
        var taken = ownLine.Categories.Select(category => Slug.FromName(category.Name).Value).ToHashSet();
        return options.Categories.Select(category => category.StoreName).FirstOrDefault(name => taken.Contains(Slug.FromName(name).Value));
    }
}
