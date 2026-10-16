namespace Grit.Modules.Basket.Contracts;

public interface IBasketReader
{
    Task<IReadOnlyList<BasketItem>> GetItemsAsync(Guid customerId, CancellationToken cancellationToken);
}
