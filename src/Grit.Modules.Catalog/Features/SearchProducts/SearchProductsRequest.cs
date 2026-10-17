using Microsoft.AspNetCore.Mvc;

namespace Grit.Modules.Catalog.Features.SearchProducts;

internal sealed record SearchProductsRequest(
    [property: FromQuery(Name = "q")] string? Query,
    [property: FromQuery(Name = "category")] string? Category,
    [property: FromQuery(Name = "minPrice")] decimal? MinPrice,
    [property: FromQuery(Name = "maxPrice")] decimal? MaxPrice,
    [property: FromQuery(Name = "attribute")] string[]? Attributes,
    [property: FromQuery(Name = "after")] string? After,
    [property: FromQuery(Name = "limit")] int? Limit);
