using Grit.Modules.Basket.Infrastructure;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Basket.Features.ChangeBasketLine;

internal sealed class ChangeBasketLineHandler(BasketStore store) : IRequestHandler<ChangeBasketLineCommand, Result>
{
    public async Task<Result> Handle(ChangeBasketLineCommand command, CancellationToken cancellationToken)
    {
        var basket = await store.GetAsync(command.CustomerId);
        var changed = basket.ChangeQuantity(command.SkuId, command.Quantity);
        if (changed.IsFailure)
        {
            return changed;
        }

        await store.SaveAsync(basket);
        return Result.Success();
    }
}
