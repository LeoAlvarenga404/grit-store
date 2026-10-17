using Grit.Modules.Basket.Contracts;
using Grit.Modules.Catalog.Contracts;
using Grit.Modules.Ordering.Domain;
using Grit.SharedKernel;
using MediatR;

namespace Grit.Modules.Ordering.Features.PlaceOrder;

internal sealed class GetCheckoutLinesHandler(IBasketReader basket, ICatalogReader catalog)
    : IRequestHandler<GetCheckoutLinesQuery, Result<IReadOnlyList<CheckoutLine>>>
{
    public async Task<Result<IReadOnlyList<CheckoutLine>>> Handle(GetCheckoutLinesQuery query, CancellationToken cancellationToken)
    {
        var items = await basket.GetItemsAsync(query.CustomerId, cancellationToken);
        var skus = (await catalog.GetSkusAsync([.. items.Select(item => item.SkuId)], cancellationToken)).ToDictionary(sku => sku.SkuId);
        if (items.Any(item => !skus.ContainsKey(item.SkuId)))
        {
            return OrderingErrors.SkuUnavailable;
        }

        IReadOnlyList<CheckoutLine> lines = [.. items.Select(item => ToLine(item, skus[item.SkuId]))];
        return Result.Success(lines);
    }

    private static CheckoutLine ToLine(BasketItem item, SkuSnapshot sku) =>
        new(sku.SkuId, sku.ProductName, item.Quantity, sku.PriceAmount, sku.PriceCurrency);
}
