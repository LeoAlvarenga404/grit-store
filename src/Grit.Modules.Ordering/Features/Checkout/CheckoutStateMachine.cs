using Grit.Modules.Inventory.Contracts;
using Grit.Modules.Ordering.Contracts;
using Grit.Modules.Ordering.Domain;
using Grit.Modules.Ordering.Infrastructure;
using Grit.Modules.Payments.Contracts;
using Grit.SharedKernel;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Ordering.Features.Checkout;

internal sealed class CheckoutStateMachine : MassTransitStateMachine<CheckoutState>
{
    public CheckoutStateMachine()
    {
        InstanceState(state => state.CurrentState);
        Event(() => OrderPlaced, e => e.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReserved, e => e.CorrelateById(context => context.Message.OrderId));
        Event(() => StockReservationFailed, e => e.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentAuthorized, e => e.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentDeclined, e => e.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderCancelled, e => e.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderShipped, e => e.CorrelateById(context => context.Message.OrderId));

        Initially(
            When(OrderPlaced)
                .Then(context => Start(context.Saga, context.Message))
                .Publish(context => new ReserveStock(context.Message.OrderId, [.. context.Message.Lines.Select(line => new StockLine(line.SkuId, line.Quantity))]))
                .TransitionTo(Reserving));

        During(Reserving,
            When(StockReserved)
                .Publish(context => new AuthorizePayment(context.Saga.CorrelationId, context.Saga.TotalAmount, context.Saga.Currency, context.Saga.PaymentToken))
                .TransitionTo(Authorizing),
            When(StockReservationFailed)
                .ThenAsync(context => ChangeOrder(context, (order, now) => order.Reject(OrderCancellationReasons.OutOfStock, now)))
                .Finalize());

        During(Authorizing,
            When(PaymentAuthorized)
                .Then(context => context.Saga.AuthorizationId = context.Message.AuthorizationId)
                .Publish(context => new CommitStock(context.Saga.CorrelationId))
                .ThenAsync(context => ChangeOrder(context, (order, now) => order.Confirm(now)))
                .TransitionTo(Confirmed),
            When(PaymentDeclined)
                .Publish(context => new ReleaseStock(context.Saga.CorrelationId))
                .ThenAsync(context => ChangeOrder(context, (order, now) => order.Reject(OrderCancellationReasons.PaymentDeclined, now)))
                .Finalize());

        // The cancel and ship endpoints already changed the order; the saga only moves the stock and refunds a cancel.
        During(Confirmed,
            When(OrderCancelled)
                .Publish(context => new ReleaseStock(context.Saga.CorrelationId))
                .Publish(context => new RefundPayment(context.Saga.CorrelationId, context.Saga.AuthorizationId!))
                .Finalize(),
            When(OrderShipped)
                .Publish(context => new ShipStock(context.Saga.CorrelationId))
                .Finalize());

        SetCompletedWhenFinalized();
    }

    public State Reserving { get; private set; } = null!;

    public State Authorizing { get; private set; } = null!;

    public State Confirmed { get; private set; } = null!;

    public Event<OrderPlaced> OrderPlaced { get; private set; } = null!;

    public Event<StockReserved> StockReserved { get; private set; } = null!;

    public Event<StockReservationFailed> StockReservationFailed { get; private set; } = null!;

    public Event<PaymentAuthorized> PaymentAuthorized { get; private set; } = null!;

    public Event<PaymentDeclined> PaymentDeclined { get; private set; } = null!;

    public Event<OrderCancelled> OrderCancelled { get; private set; } = null!;

    public Event<OrderShipped> OrderShipped { get; private set; } = null!;

    private static void Start(CheckoutState saga, OrderPlaced message)
    {
        saga.TotalAmount = message.TotalAmount;
        saga.Currency = message.Currency;
        saga.PaymentToken = message.PaymentToken;
    }

    // The consume scope's OrderingDbContext also holds the saga row and the outbox, so all three commit in one transaction.
    private static async Task ChangeOrder(BehaviorContext<CheckoutState> context, Func<Order, DateTimeOffset, Result> change)
    {
        var db = context.GetServiceOrCreateInstance<OrderingDbContext>();
        var orderId = new OrderId(context.Saga.CorrelationId);
        var order = await db.Orders.SingleAsync(order => order.Id == orderId, context.CancellationToken);
        var result = change(order, DateTimeOffset.UtcNow);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Order {order.Number} cannot follow the checkout saga: {result.Error.Code}.");
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
