using Grit.Modules.Ordering.Contracts;
using Grit.Modules.Ordering.Domain;
using Grit.Modules.Ordering.Infrastructure;
using Grit.SharedKernel;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Grit.Modules.Ordering.Features.PlaceOrder;

internal sealed class PlaceOrderHandler(
    OrderingDbContext db,
    EntityFrameworkScopedBusContextProvider<IBus, OrderingDbContext> outbox,
    TimeProvider clock) : IRequestHandler<PlaceOrderCommand, Result<PlaceOrderResponse>>
{
    private const string IdempotencyIndex = "ix_orders_customer_id_idempotency_key";

    public async Task<Result<PlaceOrderResponse>> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var existing = await db.Orders.AsNoTracking().FirstOrDefaultAsync(
            order => order.CustomerId == command.CustomerId && order.IdempotencyKey == command.IdempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            return ToResponse(existing);
        }

        var now = clock.GetUtcNow();
        var number = Order.FormatNumber(now.Year, await db.NextOrderSequenceAsync(cancellationToken));
        var lines = command.Lines.Select(ToOrderLine).ToList();
        var placed = Order.Place(command.CustomerId, number, command.IdempotencyKey, lines, command.ExpectedSubtotalAmount, now);
        if (placed.IsFailure)
        {
            return placed.Error;
        }

        db.Orders.Add(placed.Value);
        await outbox.Context.PublishEndpoint.Publish(ToOrderPlaced(placed.Value, command.PaymentToken), cancellationToken);
        return await SaveAsync(placed.Value, cancellationToken);
    }

    // A concurrent request with the same key lost the race on the unique index; its retry returns the winner's order.
    private async Task<Result<PlaceOrderResponse>> SaveAsync(Order order, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: IdempotencyIndex })
        {
            return OrderingErrors.CheckoutInProgress;
        }

        return ToResponse(order);
    }

    private static PlaceOrderResponse ToResponse(Order order) => new(order.Id.Value, order.Number);

    private static OrderLine ToOrderLine(CheckoutLine line) =>
        OrderLine.Create(line.SkuId, line.ProductName, line.Quantity, Money.Create(line.UnitPriceAmount, line.Currency).Value);

    private static OrderPlaced ToOrderPlaced(Order order, string paymentToken) =>
        new(
            order.Id.Value,
            order.CustomerId,
            [.. order.Lines.Select(line => new OrderPlacedLine(line.SkuId, line.Quantity))],
            order.Total.Amount,
            order.Total.Currency,
            paymentToken);
}
