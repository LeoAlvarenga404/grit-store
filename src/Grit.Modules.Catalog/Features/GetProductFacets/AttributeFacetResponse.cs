namespace Grit.Modules.Catalog.Features.GetProductFacets;

internal sealed record AttributeFacetResponse(string Key, IReadOnlyList<AttributeValueFacetResponse> Values);
