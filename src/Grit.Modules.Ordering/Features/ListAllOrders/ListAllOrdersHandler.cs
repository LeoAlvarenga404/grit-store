using Grit.Modules.Ordering.Domain;
using Grit.Modules.Ordering.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Ordering.Features.ListAllOrders;

internal sealed class ListAllOrdersHandler(OrderingDbContext db) : IRequestHandler<ListAllOrdersQuery, Result<ListAllOrdersResponse>>
{
    public async Task<Result<ListAllOrdersResponse>> Handle(ListAllOrdersQuery query, CancellationToken cancellationToken)
    {
        var orders = db.Orders.AsNoTracking();
        if (query.Status is not null)
        {
            var status = Enum.Parse<OrderStatus>(query.Status);
            orders = orders.Where(order => order.Status == status);
        }

        // ponytail: numbers are year + six-digit sequence, so text order is placing order; past 999999 orders a year, key on (placed_at, id).
        if (query.After is not null)
        {
            orders = orders.Where(order => order.Number.CompareTo(query.After) < 0);
        }

        var page = await orders.OrderByDescending(order => order.Number).Take(query.Limit + 1).ToListAsync(cancellationToken);
        var items = page.Take(query.Limit)
            .Select(order => new OrderOverviewResponse(order.Id.Value, order.Number, order.CustomerId, order.Status.ToString(), order.Total.Amount, order.Total.Currency, order.PlacedAt))
            .ToList();
        return new ListAllOrdersResponse(items, page.Count > query.Limit ? items[^1].Number : null);
    }
}
