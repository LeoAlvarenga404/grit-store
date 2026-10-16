using Grit.Modules.Ordering.Domain;
using Grit.Modules.Ordering.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Ordering.Features.GetOrder;

internal sealed class GetOrderHandler(OrderingDbContext db) : IRequestHandler<GetOrderQuery, Result<OrderResponse>>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(query.OrderId);
        var order = await db.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .Include(order => order.History)
            .SingleOrDefaultAsync(order => order.Id == orderId && order.CustomerId == query.CustomerId, cancellationToken);
        if (order is null)
        {
            return OrderingErrors.OrderNotFound;
        }

        return OrderResponse.From(order);
    }
}
