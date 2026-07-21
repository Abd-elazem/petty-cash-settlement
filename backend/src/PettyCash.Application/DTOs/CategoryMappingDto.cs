namespace PettyCash.Application.DTOs;

/// <summary>
/// Public API projection of a CategoryMapping row. Deliberately exposes only the three
/// fields the SPA needs: the code to submit, the human-readable label to display, and the
/// flag that controls whether Car Plate / Odometer fields appear on the line form.
///
/// Intentionally does NOT expose ExpenseMainAccount, DimensionDefaults, SalesTaxGroup, or
/// ItemSalesTaxGroup — those are internal accounting fields used by Phase 2 journal creation
/// and must not be sent to the browser (per the approved design decision for this slice).
/// </summary>
public sealed record CategoryMappingDto(
    string CategoryCode,
    string DisplayName,
    bool KmRequired);
