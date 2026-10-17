namespace Grit.Modules.Catalog.Features.SearchProducts;

// Column order matches the SQL: Dapper binds record constructors by position and name.
internal sealed record SearchProductsRow(Guid Id, string Slug, string Name, decimal PriceAmount, bool HasPriceRange, decimal SortValue, string Images, string Colors);
