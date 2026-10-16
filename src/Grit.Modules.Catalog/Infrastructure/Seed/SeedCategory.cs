namespace Grit.Modules.Catalog.Infrastructure.Seed;

internal sealed record SeedCategory(string Name, IReadOnlyList<SeedProduct> Products);
