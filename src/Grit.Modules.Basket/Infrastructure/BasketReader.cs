using Grit.Modules.Basket.Contracts;

namespace Grit.Modules.Basket.Infrastructure;

internal sealed class BasketReader(BasketStore store) : IBasketReader
{
    public async Task<IReadOnlyList<BasketItem>> GetItemsAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var basket = await store.GetAsync(customerId);
        return basket.Lines.Select(line => new BasketItem(line.SkuId, line.Quantity)).ToList();
    }
}
