using Grit.Modules.Basket.Domain;
using Grit.SharedKernel;

namespace Grit.Modules.Basket.Tests.Domain;

public class BasketTests
{
    private readonly Guid _sku = Guid.NewGuid();
    private readonly CustomerBasket _basket = CustomerBasket.Restore(Guid.NewGuid(), []);

    [Fact]
    public void Add_NewSku_AddsTheLine()
    {
        _basket.Add(Line(_sku, 2, 99.90m)).IsSuccess.ShouldBeTrue();

        _basket.Lines.ShouldHaveSingleItem().Quantity.ShouldBe(2);
    }

    [Fact]
    public void Add_SameSkuAgain_SumsTheQuantityAndKeepsTheNewestPrice()
    {
        _basket.Add(Line(_sku, 2, 99.90m));

        _basket.Add(Line(_sku, 3, 89.90m)).IsSuccess.ShouldBeTrue();

        var line = _basket.Lines.ShouldHaveSingleItem();
        line.Quantity.ShouldBe(5);
        line.UnitPrice.Amount.ShouldBe(89.90m);
    }

    [Fact]
    public void Add_SameSkuBeyondTenUnits_ReturnsQuantityLimit()
    {
        _basket.Add(Line(_sku, 6, 99.90m));

        _basket.Add(Line(_sku, 5, 99.90m)).Error.Code.ShouldBe("basket.quantity_limit");
        _basket.Lines.Single().Quantity.ShouldBe(6);
    }

    [Fact]
    public void Add_FiftyFirstSku_ReturnsLineLimit()
    {
        for (var index = 0; index < CustomerBasket.MaxLines; index++)
        {
            _basket.Add(Line(Guid.NewGuid(), 1, 10m)).IsSuccess.ShouldBeTrue();
        }

        _basket.Add(Line(_sku, 1, 10m)).Error.Code.ShouldBe("basket.line_limit");
    }

    [Fact]
    public void ChangeQuantity_ExistingLine_SetsTheQuantityUpToTen()
    {
        _basket.Add(Line(_sku, 2, 99.90m));

        _basket.ChangeQuantity(_sku, 10).IsSuccess.ShouldBeTrue();

        _basket.Lines.Single().Quantity.ShouldBe(10);
    }

    [Fact]
    public void ChangeQuantity_MissingLine_ReturnsLineNotFound()
    {
        _basket.ChangeQuantity(_sku, 1).Error.Code.ShouldBe("basket.line_not_found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void ChangeQuantity_OutOfRange_ReturnsQuantityLimit(int quantity)
    {
        _basket.Add(Line(_sku, 2, 99.90m));

        _basket.ChangeQuantity(_sku, quantity).Error.Code.ShouldBe("basket.quantity_limit");
    }

    [Fact]
    public void Remove_PresentAndMissingSku_RemovesOnlyThePresentLine()
    {
        var other = Guid.NewGuid();
        _basket.Add(Line(_sku, 1, 99.90m));
        _basket.Add(Line(other, 1, 99.90m));

        _basket.Remove(_sku);
        _basket.Remove(Guid.NewGuid());

        _basket.Lines.ShouldHaveSingleItem().SkuId.ShouldBe(other);
    }

    private static BasketLine Line(Guid skuId, int quantity, decimal price) =>
        new(skuId, quantity, Money.Create(price, "BRL").Value, "Camiseta Dry Fit", "camiseta-dry-fit");
}
