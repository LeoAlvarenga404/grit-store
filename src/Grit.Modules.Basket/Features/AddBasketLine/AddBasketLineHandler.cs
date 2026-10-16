using Grit.Modules.Basket.Domain;
using Grit.Modules.Basket.Infrastructure;
using Grit.Modules.Catalog.Contracts;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Basket.Features.AddBasketLine;

// The catalog read is this command's only connection; the basket itself is in Redis (ADR 0005).
internal sealed class AddBasketLineHandler(BasketStore store, ICatalogReader catalog) : IRequestHandler<AddBasketLineCommand, Result>
{
    public async Task<Result> Handle(AddBasketLineCommand command, CancellationToken cancellationToken)
    {
        var skus = await catalog.GetSkusAsync([command.SkuId], cancellationToken);
        if (skus.Count == 0)
        {
            return BasketErrors.SkuNotFound;
        }

        var sku = skus[0];
        var basket = await store.GetAsync(command.CustomerId);
        var price = Money.Create(sku.PriceAmount, sku.PriceCurrency).Value;
        var added = basket.Add(new BasketLine(sku.SkuId, command.Quantity, price, sku.ProductName, sku.ProductSlug));
        if (added.IsFailure)
        {
            return added;
        }

        await store.SaveAsync(basket);
        return Result.Success();
    }
}
