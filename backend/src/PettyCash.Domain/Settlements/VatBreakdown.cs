using PettyCash.Domain.Common;
using PettyCash.Domain.Exceptions;

namespace PettyCash.Domain.Settlements;

/// <summary>
/// Gross/VAT/Net split for a line. Formula per Guide §5.3: VAT = round(Gross × rate / (100 + rate), 2),
/// Net = Gross − VAT. The rate itself is NEVER hard-coded here — the Guide explicitly requires it be
/// a configuration value (currently 14% for Egypt), so callers (Application layer, reading config)
/// must pass it in. This class only knows the formula, not the number.
/// </summary>
public sealed class VatBreakdown : ValueObject
{
    public decimal Gross { get; }
    public decimal Vat { get; }
    public decimal Net { get; }

    private VatBreakdown(decimal gross, decimal vat, decimal net)
    {
        Gross = gross;
        Vat = vat;
        Net = net;
    }

    /// <summary>Used when the line's receipt is not a valid tax invoice — no split, full gross is the net expense.</summary>
    public static VatBreakdown NotApplicable(decimal gross)
    {
        if (gross <= 0)
        {
            throw new DomainValidationException("Gross amount must be greater than zero.");
        }

        return new VatBreakdown(gross, 0m, gross);
    }

    public static VatBreakdown Calculate(decimal gross, decimal vatRatePercent)
    {
        if (gross <= 0)
        {
            throw new DomainValidationException("Gross amount must be greater than zero.");
        }

        if (vatRatePercent <= 0 || vatRatePercent >= 100)
        {
            throw new DomainValidationException("VAT rate must be between 0 and 100 (exclusive).");
        }

        var vat = Math.Round(gross * vatRatePercent / (100 + vatRatePercent), 2, MidpointRounding.AwayFromZero);
        var net = gross - vat;
        return new VatBreakdown(gross, vat, net);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Gross;
        yield return Vat;
        yield return Net;
    }
}
