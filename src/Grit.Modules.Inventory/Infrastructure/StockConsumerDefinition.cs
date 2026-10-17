using MassTransit;

namespace Grit.Modules.Inventory.Infrastructure;

internal sealed class StockConsumerDefinition : ConsumerDefinition<StockConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<StockConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(100, 500, 1000, 2000));
        endpointConfigurator.UseEntityFrameworkOutbox<InventoryDbContext>(context);
    }
}
