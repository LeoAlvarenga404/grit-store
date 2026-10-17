using Grit.Modules.Ordering.Contracts;
using Grit.Modules.Ordering.Domain;
using Grit.Modules.Ordering.Infrastructure;
using Grit.SharedKernel;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Ordering.Features.CancelOrder;

internal sealed class CancelOrderHandler(
    OrderingDbContext db,
    EntityFrameworkScopedBusContextProvider<IBus, OrderingDbContext> outbox,
    TimeProvider clock) : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        // Two cancels of one order must not both pass the Confirmed check: the row lock makes the second read the first one's result.
        await db.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM ordering.orders WHERE id = {command.OrderId} AND customer_id = {command.CustomerId} FOR UPDATE",
            cancellationToken);
        var orderId = new OrderId(command.OrderId);
        var order = await db.Orders.SingleOrDefaultAsync(order => order.Id == orderId && order.CustomerId == command.CustomerId, cancellationToken);
        if (order is null)
        {
            return OrderingErrors.OrderNotFound;
        }

        var cancelled = order.Cancel(clock.GetUtcNow());
        if (cancelled.IsFailure)
        {
            return cancelled;
        }

        await outbox.Context.PublishEndpoint.Publish(new OrderCancelled(command.OrderId), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
