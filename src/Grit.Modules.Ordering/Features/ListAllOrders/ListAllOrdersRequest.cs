using Microsoft.AspNetCore.Mvc;

namespace Grit.Modules.Ordering.Features.ListAllOrders;

internal sealed record ListAllOrdersRequest(
    [property: FromQuery(Name = "status")] string? Status,
    [property: FromQuery(Name = "after")] string? After,
    [property: FromQuery(Name = "limit")] int? Limit);
