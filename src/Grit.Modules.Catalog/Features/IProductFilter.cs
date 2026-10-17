namespace Grit.Modules.Catalog.Features;

// Shared by the search and the facets queries, so both validate and filter the same way.
internal interface IProductFilter
{
    string? Query { get; }

    string? Category { get; }

    decimal? MinPrice { get; }

    decimal? MaxPrice { get; }

    IReadOnlyList<string> Attributes { get; }
}
