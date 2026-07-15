using PettyCash.Domain.Exceptions;

namespace PettyCash.Domain.Settlements;

/// <summary>
/// A single spending line within a Settlement. Not independently addressable —
/// all mutation happens through the owning <see cref="Settlement"/> aggregate,
/// which is why the constructor and mutators are internal, not public.
///
/// ExpenseMainAccountSnapshot / DimensionDefaultsSnapshot are copies of
/// CategoryMapping resolved by the Application layer at write time — see
/// DECISIONS.md D-004. This class never looks mapping data up itself.
/// </summary>
public sealed class SettlementLine
{
    public Guid LineId { get; private set; }
    public int LineNo { get; internal set; }
    public string CategoryCode { get; private set; }
    public string ExpenseMainAccountSnapshot { get; private set; }
    public string DimensionDefaultsSnapshot { get; private set; }
    public Money GrossAmount { get; private set; }
    public bool IsVat { get; private set; }
    public VatBreakdown VatBreakdown { get; private set; }
    public string? Notes { get; private set; }
    public OdometerReading? Odometer { get; private set; }

    // Parameterless constructor for Infrastructure-side materialization only (EF Core
    // reading rows back from Postgres) — same pattern and same reasoning as Settlement's
    // own private constructor (Milestone 0.2). Added at Sprint 4 (Infrastructure) because
    // the internal constructor below takes transient inputs (vatRatePercent, kmRequired)
    // that aren't stored columns, so EF has no way to bind to it. Rehydration always
    // restores an already-valid state, so bypassing Apply()'s validation here is safe —
    // exactly the same argument already accepted for Settlement.
    private SettlementLine()
    {
        CategoryCode = string.Empty;
        ExpenseMainAccountSnapshot = string.Empty;
        DimensionDefaultsSnapshot = string.Empty;
        GrossAmount = null!;
        VatBreakdown = null!;
    }

    internal SettlementLine(
        int lineNo,
        string categoryCode,
        string expenseMainAccountSnapshot,
        string dimensionDefaultsSnapshot,
        Money grossAmount,
        bool isVat,
        decimal vatRatePercent,
        bool kmRequired,
        OdometerReading? odometer,
        string? notes)
    {
        LineId = Guid.NewGuid();
        LineNo = lineNo;
        CategoryCode = string.Empty;
        ExpenseMainAccountSnapshot = string.Empty;
        DimensionDefaultsSnapshot = string.Empty;
        GrossAmount = grossAmount;
        VatBreakdown = VatBreakdown.NotApplicable(grossAmount.Amount);

        Apply(categoryCode, expenseMainAccountSnapshot, dimensionDefaultsSnapshot,
            grossAmount, isVat, vatRatePercent, kmRequired, odometer, notes);
    }

    internal void Update(
        string categoryCode,
        string expenseMainAccountSnapshot,
        string dimensionDefaultsSnapshot,
        Money grossAmount,
        bool isVat,
        decimal vatRatePercent,
        bool kmRequired,
        OdometerReading? odometer,
        string? notes)
    {
        Apply(categoryCode, expenseMainAccountSnapshot, dimensionDefaultsSnapshot,
            grossAmount, isVat, vatRatePercent, kmRequired, odometer, notes);
    }

    private void Apply(
        string categoryCode,
        string expenseMainAccountSnapshot,
        string dimensionDefaultsSnapshot,
        Money grossAmount,
        bool isVat,
        decimal vatRatePercent,
        bool kmRequired,
        OdometerReading? odometer,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(categoryCode))
        {
            throw new DomainValidationException("Category is required.");
        }

        if (string.IsNullOrWhiteSpace(expenseMainAccountSnapshot))
        {
            throw new DomainValidationException(
                $"No expense account is mapped for category '{categoryCode}'.");
        }

        // Fuel lines: Guide §5.3 — car plate + odometer appear only when the category requires it.
        if (kmRequired && odometer is null)
        {
            throw new DomainValidationException(
                $"Category '{categoryCode}' requires a car plate and odometer reading.");
        }

        if (!kmRequired && odometer is not null)
        {
            throw new DomainValidationException(
                $"Category '{categoryCode}' does not accept car plate/odometer data.");
        }

        CategoryCode = categoryCode;
        ExpenseMainAccountSnapshot = expenseMainAccountSnapshot;
        DimensionDefaultsSnapshot = dimensionDefaultsSnapshot;
        GrossAmount = grossAmount;
        IsVat = isVat;
        VatBreakdown = isVat
            ? VatBreakdown.Calculate(grossAmount.Amount, vatRatePercent)
            : VatBreakdown.NotApplicable(grossAmount.Amount);
        Odometer = odometer;
        Notes = notes;
    }
}
