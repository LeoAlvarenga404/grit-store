using Grit.Modules.Ordering.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Ordering.Features.ListOrders;

// ponytail: no paging; add a keyset cursor when a customer has hundreds of orders.
internal sealed class ListOrdersHandler(OrderingDbContext db) : IRequestHandler<ListOrdersQuery, IReadOnlyList<OrderSummaryResponse>>
{
    public async Task<IReadOnlyList<OrderSummaryResponse>> Handle(ListOrdersQuery query, CancellationToken cancellationToken)
    {
        var orders = await db.Orders
            .AsNoTracking()
            .Where(order => order.CustomerId == query.CustomerId)
            .OrderByDescending(order => order.PlacedAt)
            .ToListAsync(cancellationToken);
        return [.. orders.Select(order => new OrderSummaryResponse(order.Id.Value, order.Number, order.Status.ToString(), order.Total.Amount, order.Total.Currency, order.PlacedAt))];
    }
}
