using Grit.Modules.Basket.Infrastructure;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Basket.Features.RemoveBasketLine;

internal sealed class RemoveBasketLineHandler(BasketStore store) : IRequestHandler<RemoveBasketLineCommand, Result>
{
    public async Task<Result> Handle(RemoveBasketLineCommand command, CancellationToken cancellationToken)
    {
        var basket = await store.GetAsync(command.CustomerId);
        basket.Remove(command.SkuId);
        await store.SaveAsync(basket);
        return Result.Success();
    }
}
