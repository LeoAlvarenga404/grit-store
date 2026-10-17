namespace Grit.Modules.Ordering.Features.ListAllOrders;

internal sealed record ListAllOrdersResponse(IReadOnlyList<OrderOverviewResponse> Items, string? NextCursor);
