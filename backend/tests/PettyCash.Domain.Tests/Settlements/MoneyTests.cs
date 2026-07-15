using PettyCash.Domain.Exceptions;
using PettyCash.Domain.Settlements;
using Xunit;

namespace PettyCash.Domain.Tests.Settlements;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithZeroOrNegativeAmount_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new Money(0m));
        Assert.Throws<DomainValidationException>(() => new Money(-1m));
    }

    [Fact]
    public void Constructor_WithUnsupportedCurrency_Throws()
    {
        // Single-currency MVP per ASSUMPTIONS.md A-005.
        Assert.Throws<DomainValidationException>(() => new Money(100m, "USD"));
    }

    [Fact]
    public void Addition_SameCurrency_Sums()
    {
        var a = new Money(100m);
        var b = new Money(50.50m);

        var result = a + b;

        Assert.Equal(150.50m, result.Amount);
    }

    [Fact]
    public void Equality_IsByValue_NotReference()
    {
        var a = new Money(100m);
        var b = new Money(100m);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }
}
