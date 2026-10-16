using Grit.SharedKernel;

namespace Grit.Modules.Ordering.Domain;

internal sealed class OrderLine
{
    // EF cannot bind a complex property such as UnitPrice to a constructor parameter; it sets UnitPrice right after.
    private OrderLine(Guid skuId, string productName, int quantity)
    {
        SkuId = skuId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = null!;
    }

    public Guid SkuId { get; }

    public string ProductName { get; }

    public int Quantity { get; }

    public Money UnitPrice { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    public static OrderLine Create(Guid skuId, string productName, int quantity, Money unitPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        return new OrderLine(skuId, productName, quantity) { UnitPrice = unitPrice };
    }
}
