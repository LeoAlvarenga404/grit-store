namespace Grit.Modules.Catalog.Infrastructure.Seed;

// Images are file names in the images folder next to the seed file; a hand-written seed leaves them out.
internal sealed record SeedProduct(
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Attributes,
    IReadOnlyList<SeedSku> Skus,
    IReadOnlyList<string>? Images = null);
