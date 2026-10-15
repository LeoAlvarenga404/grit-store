namespace Grit.SharedKernel;

public sealed record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
        {
            return new Error("money.negative_amount", "Amount cannot be negative.", ErrorType.Validation);
        }

        // Two decimal places fit BRL and USD; a currency with other minor units (JPY, BHD) needs ISO 4217 minor-unit data.
        if (decimal.Round(amount, 2) != amount)
        {
            return new Error("money.too_many_decimal_places", "Amount cannot have more than two decimal places.", ErrorType.Validation);
        }

        if (currency.Length != 3 || !currency.All(char.IsAsciiLetterUpper))
        {
            return new Error("money.invalid_currency", "Currency must be three uppercase ASCII letters.", ErrorType.Validation);
        }

        return new Money(amount, currency);
    }

    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other.Currency != Currency)
        {
            throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}.");
        }

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        return new Money(Amount * quantity, Currency);
    }
}
