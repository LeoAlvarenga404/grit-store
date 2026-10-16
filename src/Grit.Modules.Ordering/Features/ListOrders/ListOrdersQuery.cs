using MediatR;

namespace Grit.Modules.Ordering.Features.ListOrders;

internal sealed record ListOrdersQuery(Guid CustomerId) : IRequest<IReadOnlyList<OrderSummaryResponse>>;
