namespace Grit.Modules.Catalog.Features.GetProductFacets;

internal sealed record GetProductFacetsResponse(
    IReadOnlyList<CategoryFacetResponse> Categories,
    IReadOnlyList<PriceFacetResponse> Prices,
    IReadOnlyList<AttributeFacetResponse> Attributes);
