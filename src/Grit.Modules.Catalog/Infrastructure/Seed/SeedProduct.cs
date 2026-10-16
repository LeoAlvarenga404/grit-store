namespace Grit.Modules.Catalog.Infrastructure.Seed;

internal sealed record SeedProduct(
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Attributes,
    IReadOnlyList<SeedSku> Skus);
