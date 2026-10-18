namespace Grit.Modules.Catalog.Features.SearchProducts;

// Aligned with the item: the cover's key and, for each of its colours in order, that colour's first image key.
internal sealed record SearchItemImageKeys(string? Cover, IReadOnlyList<string?> Colors);
