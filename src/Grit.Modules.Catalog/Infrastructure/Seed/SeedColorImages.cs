namespace Grit.Modules.Catalog.Infrastructure.Seed;

// The images of one colour: Color is the SKUs' "cor" value, or null for a product whose SKUs have no colour.
internal sealed record SeedColorImages(string? Color, IReadOnlyList<string> Files);
