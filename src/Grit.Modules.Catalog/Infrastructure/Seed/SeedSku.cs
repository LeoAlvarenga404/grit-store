namespace Grit.Modules.Catalog.Infrastructure.Seed;

internal sealed record SeedSku(IReadOnlyDictionary<string, string> Attributes, decimal PriceAmount);
