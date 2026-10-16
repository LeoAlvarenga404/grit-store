namespace Grit.Modules.Catalog.Features.GetProductFacets;

internal sealed record FacetRow(string Kind, string Key, string? Value, long Count);
