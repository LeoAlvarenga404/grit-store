using Grit.SharedKernel;

namespace Grit.Modules.Basket.Domain;

internal sealed class CustomerBasket
{
    public const int MaxQuantityPerLine = 10;

    public const int MaxLines = 50;

    private readonly List<BasketLine> _lines;

    private CustomerBasket(Guid customerId, List<BasketLine> lines)
    {
        CustomerId = customerId;
        _lines = lines;
    }

    public Guid CustomerId { get; }

    public IReadOnlyList<BasketLine> Lines => _lines;

    public static CustomerBasket Restore(Guid customerId, IEnumerable<BasketLine> lines) => new(customerId, [.. lines]);

    // The newest price snapshot wins: adding again is the customer accepting today's price.
    public Result Add(BasketLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var index = _lines.FindIndex(existing => existing.SkuId == line.SkuId);
        if (index < 0)
        {
            return AddNew(line);
        }

        var quantity = _lines[index].Quantity + line.Quantity;
        if (!IsAllowed(quantity))
        {
            return BasketErrors.QuantityLimit;
        }

        _lines[index] = line with { Quantity = quantity };
        return Result.Success();
    }

    public Result ChangeQuantity(Guid skuId, int quantity)
    {
        var index = _lines.FindIndex(existing => existing.SkuId == skuId);
        if (index < 0)
        {
            return BasketErrors.LineNotFound;
        }

        if (!IsAllowed(quantity))
        {
            return BasketErrors.QuantityLimit;
        }

        _lines[index] = _lines[index] with { Quantity = quantity };
        return Result.Success();
    }

    public void Remove(Guid skuId) => _lines.RemoveAll(line => line.SkuId == skuId);

    // Only what the order bought leaves the basket: units added after the checkout stay.
    public void RemovePurchased(Guid skuId, int quantity)
    {
        var index = _lines.FindIndex(existing => existing.SkuId == skuId);
        if (index < 0)
        {
            return;
        }

        var remaining = _lines[index].Quantity - quantity;
        if (remaining <= 0)
        {
            _lines.RemoveAt(index);
            return;
        }

        _lines[index] = _lines[index] with { Quantity = remaining };
    }

    private static bool IsAllowed(int quantity) => quantity is >= 1 and <= MaxQuantityPerLine;

    private Result AddNew(BasketLine line)
    {
        if (_lines.Count >= MaxLines)
        {
            return BasketErrors.LineLimit;
        }

        if (!IsAllowed(line.Quantity))
        {
            return BasketErrors.QuantityLimit;
        }

        _lines.Add(line);
        return Result.Success();
    }
}
