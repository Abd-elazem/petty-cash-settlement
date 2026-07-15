namespace PettyCash.Domain.Settlements;

/// <summary>
/// Mirrors the Status choice list in the Guide §5.5 SharePoint header list exactly.
/// Rejected is a first-class status (see DECISIONS.md D-014), not a transient state
/// that auto-reverts to Draft.
/// </summary>
public enum SettlementStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Journalled,
    Posted
}
