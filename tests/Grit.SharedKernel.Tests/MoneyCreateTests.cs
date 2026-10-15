namespace Grit.SharedKernel.Tests;

public class MoneyCreateTests
{
    public static TheoryData<decimal> ValidAmounts => [0m, 0.01m, 10.5m, 10.500m, 199.99m];

    public static TheoryData<decimal> AmountsWithMoreThanTwoDecimalPlaces => [0.001m, 10.555m];

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

    [Fact]
    public void Create_AmountAtMaximum_ReturnsMoney()
    {
        var result = Money.Create(1_000_000_000_000m, "BRL");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Amount.ShouldBe(1_000_000_000_000m);
    }

    [Fact]
    public void Create_AmountAboveMaximum_ReturnsAmountTooLargeError()
    {
        var result = Money.Create(1_000_000_000_000.01m, "BRL");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("money.amount_too_large");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void Create_AmountAboveMaximumAndInvalidCurrency_ReturnsAmountTooLargeError()
    {
        var result = Money.Create(1_000_000_000_000.01m, "brl");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("money.amount_too_large");
    }
}
