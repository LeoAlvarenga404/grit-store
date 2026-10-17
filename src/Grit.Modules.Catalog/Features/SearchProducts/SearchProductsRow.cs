namespace Grit.Modules.Catalog.Features.SearchProducts;

internal sealed record SearchProductsRow(Guid Id, string Slug, string Name, decimal PriceAmount, decimal Rank);
