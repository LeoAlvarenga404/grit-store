using Grit.Modules.Payments.Contracts;
using Grit.Modules.Payments.Domain;
using Grit.SharedKernel;
using MassTransit;

namespace Grit.Modules.Payments.Infrastructure;

internal sealed class PaymentConsumer(IPaymentGateway gateway) : IConsumer<AuthorizePayment>, IConsumer<RefundPayment>
{
    public async Task Consume(ConsumeContext<AuthorizePayment> context)
    {
        var message = context.Message;
        var amount = Money.Create(message.Amount, message.Currency).Value;
        var authorization = await gateway.AuthorizeAsync(amount, message.PaymentToken, context.CancellationToken);
        if (authorization.IsFailure)
        {
            await context.Publish(new PaymentDeclined(message.OrderId, authorization.Error.Code), context.CancellationToken);
            return;
        }

        await context.Publish(new PaymentAuthorized(message.OrderId, authorization.Value), context.CancellationToken);
    }

    public Task Consume(ConsumeContext<RefundPayment> context) =>
        gateway.RefundAsync(context.Message.AuthorizationId, context.CancellationToken);
}
