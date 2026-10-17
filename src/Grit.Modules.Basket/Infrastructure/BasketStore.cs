using System.Text.Json;
using Grit.Modules.Basket.Domain;
using Grit.Modules.Ordering.Contracts;
using StackExchange.Redis;

namespace Grit.Modules.Basket.Infrastructure;

// ponytail: read-modify-write without WATCH; two tabs changing the same basket at once can lose one change.
internal sealed class BasketStore(IConnectionMultiplexer redis)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    public async Task<CustomerBasket> GetAsync(Guid customerId)
    {
        var value = await redis.GetDatabase().StringGetAsync(Key(customerId));
        if (value.IsNullOrEmpty)
        {
            return CustomerBasket.Restore(customerId, []);
        }

        var lines = JsonSerializer.Deserialize<List<StoredBasketLine>>(value.ToString()) ?? [];
        return CustomerBasket.Restore(customerId, lines.Select(line => line.ToDomain()));
    }

    public Task SaveAsync(CustomerBasket basket)
    {
        var json = JsonSerializer.Serialize(basket.Lines.Select(StoredBasketLine.From).ToList());
        return redis.GetDatabase().StringSetAsync(Key(basket.CustomerId), json, Lifetime, When.Always);
    }

    // OrderPlaced can arrive late or twice. The per-order mark is set first, so a redelivery changes nothing,
    // and a crash between mark and save leaves the purchased lines in the basket rather than removing new ones.
    public async Task RemovePurchasedAsync(OrderPlaced order)
    {
        var isFirstDelivery = await redis.GetDatabase().StringSetAsync($"basket-cleared:{order.OrderId:N}", 1, Lifetime, When.NotExists);
        if (!isFirstDelivery)
        {
            return;
        }

        var basket = await GetAsync(order.CustomerId);
        foreach (var line in order.Lines)
        {
            basket.RemovePurchased(line.SkuId, line.Quantity);
        }

        await SaveAsync(basket);
    }

    private static RedisKey Key(Guid customerId) => $"basket:{customerId:N}";
}
