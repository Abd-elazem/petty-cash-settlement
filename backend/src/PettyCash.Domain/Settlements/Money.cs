using PettyCash.Domain.Common;
using PettyCash.Domain.Exceptions;

namespace PettyCash.Domain.Settlements;

/// <summary>
/// A monetary amount. Single-currency for MVP (ASSUMPTIONS.md A-005, Medium risk,
/// pending client confirmation) — the supported-currency list below is the one place
/// that changes if multi-currency is ever approved.
/// </summary>
public sealed class Money : ValueObject
{
    private static readonly string[] SupportedCurrencies = { "EGP" };

    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "EGP")
    {
        if (amount <= 0)
        {
            throw new DomainValidationException("Amount must be greater than zero.");
        }

        if (!SupportedCurrencies.Contains(currency))
        {
            throw new DomainValidationException(
                $"Currency '{currency}' is not supported (see ASSUMPTIONS.md A-005).");
        }

        Amount = amount;
        Currency = currency;
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new DomainValidationException(
                $"Cannot add amounts in different currencies ('{a.Currency}' vs '{b.Currency}').");
        }

        return new Money(a.Amount + b.Amount, a.Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
