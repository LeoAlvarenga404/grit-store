using Grit.Modules.Ordering.Infrastructure;
using MassTransit;

namespace Grit.Modules.Ordering.Features.Checkout;

internal sealed class CheckoutStateDefinition : SagaDefinition<CheckoutState>
{
    protected override void ConfigureSaga(
        IReceiveEndpointConfigurator endpointConfigurator,
        ISagaConfigurator<CheckoutState> sagaConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(100, 500, 1000, 2000));
        endpointConfigurator.UseEntityFrameworkOutbox<OrderingDbContext>(context);
    }
}
