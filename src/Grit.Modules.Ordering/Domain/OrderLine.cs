using Grit.SharedKernel;

namespace Grit.Modules.Ordering.Domain;

internal sealed class OrderLine
{
    // EF cannot bind a complex property such as UnitPrice to a constructor parameter; it sets UnitPrice right after.
    private OrderLine(Guid skuId, string productName, string? productSlug, string? color, string? size, int quantity)
    {
        SkuId = skuId;
        ProductName = productName;
        ProductSlug = productSlug;
        Color = color;
        Size = size;
        Quantity = quantity;
        UnitPrice = null!;
    }

    public Guid SkuId { get; }

    public string ProductName { get; }

    // Null only on lines placed before the order kept the variant (migration OrderLineVariant).
    public string? ProductSlug { get; }

    public string? Color { get; }

    public string? Size { get; }

    public int Quantity { get; }

    public Money UnitPrice { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    public static OrderLine Create(Guid skuId, string productName, string productSlug, string? color, string? size, int quantity, Money unitPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        return new OrderLine(skuId, productName, productSlug, color, size, quantity) { UnitPrice = unitPrice };
    }
}
