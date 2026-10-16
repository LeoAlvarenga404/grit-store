namespace Grit.SharedKernel.Tests;

public class MoneyArithmeticTests
{
    public static TheoryData<int, decimal, string> QuantitiesAndLineTotals => new()
    {
        { 0, 0m, "BRL" },
        { 1, 19.99m, "USD" },
        { 3, 59.97m, "BRL" },
    };

    [Theory]
    [InlineData("BRL")]
    [InlineData("USD")]
    public void Add_SameCurrency_SumsAmounts(string currency)
    {
        var price = Money.Create(10.50m, currency).Value;
        var shipping = Money.Create(0.75m, currency).Value;

        var total = price.Add(shipping);

        total.Amount.ShouldBe(11.25m);
        total.Currency.ShouldBe(currency);
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
    {
        var real = Money.Create(1m, "BRL").Value;
        var dollar = Money.Create(1m, "USD").Value;

        Should.Throw<InvalidOperationException>(() => real.Add(dollar));
    }

    [Fact]
    public void Add_ResultAboveMaximum_Throws()
    {
        var total = Money.Create(999_999_999_999.99m, "BRL").Value;
        var extra = Money.Create(0.02m, "BRL").Value;

        Should.Throw<OverflowException>(() => total.Add(extra));
    }

    [Fact]
    public void Add_ResultAtMaximum_ReturnsMoney()
    {
        var total = Money.Create(999_999_999_999.99m, "BRL").Value;
        var extra = Money.Create(0.01m, "BRL").Value;

        var sum = total.Add(extra);

        sum.Amount.ShouldBe(1_000_000_000_000m);
    }

    [Theory]
    [MemberData(nameof(QuantitiesAndLineTotals))]
    public void Multiply_Quantity_MultipliesAmount(int quantity, decimal expectedAmount, string currency)
    {
        var unitPrice = Money.Create(19.99m, currency).Value;

        var lineTotal = unitPrice.Multiply(quantity);

        lineTotal.Amount.ShouldBe(expectedAmount);
        lineTotal.Currency.ShouldBe(currency);
    }

    [Fact]
    public void Multiply_ResultAboveMaximum_Throws()
    {
        var unitPrice = Money.Create(500_000_000_000.01m, "BRL").Value;

        Should.Throw<OverflowException>(() => unitPrice.Multiply(2));
    }

    [Fact]
    public void Multiply_LargeQuantity_KeepsCents()
    {
        var unitPrice = Money.Create(0.01m, "BRL").Value;

        var lineTotal = unitPrice.Multiply(int.MaxValue);

        lineTotal.Amount.ShouldBe(21_474_836.47m);
    }

    [Fact]
    public void Multiply_NegativeQuantity_Throws()
    {
        var unitPrice = Money.Create(19.99m, "BRL").Value;

        Should.Throw<ArgumentOutOfRangeException>(() => unitPrice.Multiply(-1));
    }

    [Fact]
    public void Equals_SameAmountAndCurrency_AreEqual()
    {
        var first = Money.Create(10.5m, "BRL").Value;
        var second = Money.Create(10.50m, "BRL").Value;

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentAmount_AreNotEqual()
    {
        var ten = Money.Create(10m, "BRL").Value;
        var eleven = Money.Create(11m, "BRL").Value;

        ten.ShouldNotBe(eleven);
    }

    [Fact]
    public void Equals_DifferentCurrency_AreNotEqual()
    {
        var real = Money.Create(10m, "BRL").Value;
        var dollar = Money.Create(10m, "USD").Value;

        real.ShouldNotBe(dollar);
    }
}
