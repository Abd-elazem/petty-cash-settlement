using PettyCash.Domain.Exceptions;
using PettyCash.Domain.Settlements;
using Xunit;

namespace PettyCash.Domain.Tests.Settlements;

public class VatBreakdownTests
{
    [Fact]
    public void Calculate_MatchesGuideFormula_100GrossAt14Percent()
    {
        // Guide §5.3: VAT = round(Gross × 14 / 114, 2), Net = Gross − VAT.
        // 100 × 14 / 114 = 12.28 (rounded).
        var result = VatBreakdown.Calculate(100m, 14m);

        Assert.Equal(100m, result.Gross);
        Assert.Equal(12.28m, result.Vat);
        Assert.Equal(87.72m, result.Net);
    }

    [Fact]
    public void Calculate_1000GrossAt14Percent_RoundsCorrectly()
    {
        // 1000 × 14 / 114 = 122.807... → 122.81
        var result = VatBreakdown.Calculate(1000m, 14m);

        Assert.Equal(122.81m, result.Vat);
        Assert.Equal(877.19m, result.Net);
    }

    [Fact]
    public void NotApplicable_SetsFullGrossAsNet()
    {
        var result = VatBreakdown.NotApplicable(200m);

        Assert.Equal(200m, result.Gross);
        Assert.Equal(0m, result.Vat);
        Assert.Equal(200m, result.Net);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Calculate_WithNonPositiveGross_Throws(decimal gross)
    {
        Assert.Throws<DomainValidationException>(() => VatBreakdown.Calculate(gross, 14m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-5)]
    public void Calculate_WithInvalidRate_Throws(decimal rate)
    {
        Assert.Throws<DomainValidationException>(() => VatBreakdown.Calculate(100m, rate));
    }

    [Fact]
    public void Calculate_RateIsCallerSupplied_NotHardCoded()
    {
        // Guide §5.3 explicitly requires the VAT rate be a configuration value, not a
        // hard-coded number. This test just proves the API takes the rate as a parameter —
        // there is no default/hidden 14% anywhere in VatBreakdown itself.
        var atFivePercent = VatBreakdown.Calculate(100m, 5m);
        var atFourteenPercent = VatBreakdown.Calculate(100m, 14m);

        Assert.NotEqual(atFivePercent.Vat, atFourteenPercent.Vat);
    }
}
