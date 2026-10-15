namespace Grit.SharedKernel.Tests;

public class MoneyTests
{
    public static TheoryData<decimal> ValidAmounts => [0m, 0.01m, 10.5m, 10.500m, 199.99m];

    public static TheoryData<decimal> AmountsWithMoreThanTwoDecimalPlaces => [0.001m, 10.555m];

    public static TheoryData<int, decimal, string> QuantitiesAndLineTotals => new()
    {
        { 0, 0m, "BRL" },
        { 1, 19.99m, "USD" },
        { 3, 59.97m, "BRL" },
    };

    [Theory]
    [MemberData(nameof(ValidAmounts))]
    public void Create_ValidAmount_ReturnsMoney(decimal amount)
    {
        var result = Money.Create(amount, "BRL");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Amount.ShouldBe(amount);
        result.Value.Currency.ShouldBe("BRL");
    }

    [Theory]
    [InlineData("BRL")]
    [InlineData("USD")]
    public void Create_ValidCurrency_KeepsCurrency(string currency)
    {
        var result = Money.Create(10m, currency);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Amount.ShouldBe(10m);
        result.Value.Currency.ShouldBe(currency);
    }

    [Fact]
    public void Create_NegativeAmount_ReturnsValidationError()
    {
        var result = Money.Create(-0.01m, "BRL");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("money.negative_amount");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Theory]
    [MemberData(nameof(AmountsWithMoreThanTwoDecimalPlaces))]
    public void Create_MoreThanTwoDecimalPlaces_ReturnsValidationError(decimal amount)
    {
        var result = Money.Create(amount, "BRL");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("money.too_many_decimal_places");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("BR")]
    [InlineData("BRLL")]
    [InlineData("brl")]
    [InlineData("B1L")]
    [InlineData("ÇRL")]
    public void Create_InvalidCurrency_ReturnsValidationError(string currency)
    {
        var result = Money.Create(10m, currency);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("money.invalid_currency");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void Create_NegativeAmountAndInvalidCurrency_ReturnsNegativeAmountError()
    {
        var result = Money.Create(-0.001m, "brl");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("money.negative_amount");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void Create_TooManyDecimalPlacesAndInvalidCurrency_ReturnsDecimalPlacesError()
    {
        var result = Money.Create(0.001m, "brl");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("money.too_many_decimal_places");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

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
