namespace Grit.Modules.Catalog.Features.GetProductFacets;

internal sealed record PriceFacetResponse(decimal MinAmount, decimal? MaxAmount, long Count);
