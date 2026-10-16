using Grit.Modules.Basket.Domain;
using Grit.Modules.Basket.Infrastructure;
using Grit.Modules.Catalog.Contracts;
using MediatR;

namespace Grit.Modules.Basket.Features.GetBasket;

internal sealed class GetBasketHandler(BasketStore store, ICatalogReader catalog) : IRequestHandler<GetBasketQuery, BasketResponse>
{
    public async Task<BasketResponse> Handle(GetBasketQuery query, CancellationToken cancellationToken)
    {
        var basket = await store.GetAsync(query.CustomerId);
        var skuIds = basket.Lines.Select(line => line.SkuId).ToList();
        var current = (await catalog.GetSkusAsync(skuIds, cancellationToken)).ToDictionary(sku => sku.SkuId);
        var lines = basket.Lines.Select(line => ToResponse(line, current.GetValueOrDefault(line.SkuId))).ToList();
        var subtotal = lines.Where(line => line.IsAvailable).Sum(line => line.CurrentUnitPriceAmount!.Value * line.Quantity);
        return new BasketResponse(lines, subtotal, BasketResponse.StoreCurrency);
    }

    private static BasketLineResponse ToResponse(BasketLine line, SkuSnapshot? sku) =>
        new(
            line.SkuId,
            line.ProductName,
            line.ProductSlug,
            line.Quantity,
            line.UnitPrice.Amount,
            sku?.PriceAmount,
            sku is not null && sku.PriceAmount != line.UnitPrice.Amount,
            sku is not null);
}
