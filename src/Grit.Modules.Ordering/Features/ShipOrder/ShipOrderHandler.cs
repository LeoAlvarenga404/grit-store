using Grit.Modules.Ordering.Contracts;
using Grit.Modules.Ordering.Domain;
using Grit.Modules.Ordering.Infrastructure;
using Grit.SharedKernel;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Ordering.Features.ShipOrder;

internal sealed class ShipOrderHandler(
    OrderingDbContext db,
    EntityFrameworkScopedBusContextProvider<IBus, OrderingDbContext> outbox,
    TimeProvider clock) : IRequestHandler<ShipOrderCommand, Result>
{
    public async Task<Result> Handle(ShipOrderCommand command, CancellationToken cancellationToken)
    {
        // The same row lock as the customer's cancel: a ship and a cancel of one order never both pass the Confirmed check.
        await db.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM ordering.orders WHERE id = {command.OrderId} FOR UPDATE",
            cancellationToken);
        var orderId = new OrderId(command.OrderId);
        var order = await db.Orders.SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);
        if (order is null)
        {
            return OrderingErrors.OrderNotFound;
        }

        var shipped = order.Ship(clock.GetUtcNow());
        if (shipped.IsFailure)
        {
            return shipped;
        }

        await outbox.Context.PublishEndpoint.Publish(new OrderShipped(command.OrderId), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
