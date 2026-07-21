namespace PettyCash.Application.Abstractions;

/// <summary>
/// Read-only projection of a CategoryMapping row (Guide §5.5, Finance-owned reference data).
/// Deliberately NOT a Domain entity: it has no invariants or behavior of its own — it's just
/// data the Application layer resolves and hands to Settlement.AddLine/UpdateLine as already-
/// validated snapshot values (see DECISIONS.md D-004). Modeling it in Domain would add an
/// aggregate with no real business rules to enforce.
/// </summary>
public sealed record CategoryMappingReadModel(
    string CategoryCode,
    string DisplayName,
    string ExpenseMainAccount,
    string? DimensionDefaults,
    string? SalesTaxGroup,
    string? ItemSalesTaxGroup,
    bool KmRequired,
    bool Active);

/// <summary>Read-only projection of an AppUserProfile row (Guide §5.2). Same reasoning as above.</summary>
public sealed record AppUserProfileReadModel(
    string AppUserId,
    string DisplayName,
    string WorkerId,
    string ApproverEmail,
    bool Active);
