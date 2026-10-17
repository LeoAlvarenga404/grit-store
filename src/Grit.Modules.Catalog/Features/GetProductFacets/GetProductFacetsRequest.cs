using Microsoft.AspNetCore.Mvc;

namespace Grit.Modules.Catalog.Features.GetProductFacets;

internal sealed record GetProductFacetsRequest(
    [property: FromQuery(Name = "q")] string? Query,
    [property: FromQuery(Name = "category")] string? Category,
    [property: FromQuery(Name = "minPrice")] decimal? MinPrice,
    [property: FromQuery(Name = "maxPrice")] decimal? MaxPrice,
    [property: FromQuery(Name = "attribute")] string[]? Attributes);
