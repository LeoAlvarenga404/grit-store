using Grit.Modules.Ordering.Contracts;
using MassTransit;

namespace Grit.Modules.Basket.Infrastructure;

internal sealed class ClearBasketConsumer(BasketStore store) : IConsumer<OrderPlaced>
{
    public Task Consume(ConsumeContext<OrderPlaced> context) => store.RemovePurchasedAsync(context.Message);
}
