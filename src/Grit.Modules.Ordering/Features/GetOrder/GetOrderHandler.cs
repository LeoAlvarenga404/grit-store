using Grit.Modules.Catalog.Contracts;
using Grit.Modules.Ordering.Domain;
using Grit.Modules.Ordering.Infrastructure;
using Grit.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Ordering.Features.GetOrder;

internal sealed class GetOrderHandler(OrderingDbContext db, ICatalogReader catalog) : IRequestHandler<GetOrderQuery, Result<OrderResponse>>
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

        // Catalog image URLs expire after an hour, so the order keeps none and asks for the SKUs' current photos on each read.
        var skus = await catalog.GetSkusAsync([.. order.Lines.Select(line => line.SkuId)], cancellationToken);
        return OrderResponse.From(order, skus.ToDictionary(sku => sku.SkuId, sku => sku.ImageUrl));
    }
}
