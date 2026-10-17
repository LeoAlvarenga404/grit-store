namespace Grit.Modules.Basket.Features.GetBasket;

internal sealed record BasketResponse(IReadOnlyList<BasketLineResponse> Lines, decimal SubtotalAmount, string Currency)
{
    // The store sells only in BRL (ADR 0012).
    public const string StoreCurrency = "BRL";
}
