namespace Grit.Modules.Catalog.Features.SearchProducts;

internal static class ProductSort
{
    public const string Relevance = "relevance";

    public const string PriceAscending = "price_asc";

    public const string PriceDescending = "price_desc";

    public const string Message = "Use relevance, price_asc or price_desc.";

    public static readonly IReadOnlyList<string> All = [Relevance, PriceAscending, PriceDescending];
}
