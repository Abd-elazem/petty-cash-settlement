namespace PettyCash.Application.Abstractions;

/// <summary>
/// Maps to the RBAC model in ARCHITECTURE.md §7. System represents the Power Automate
/// flow calling back into the API (D-006) — it is a role, not a special-cased bypass,
/// so the same authorization policy code path handles it.
/// </summary>
public enum UserRole
{
    Spender,
    Approver,
    ApAccountant,
    FinanceContentOwner,
    AppAdmin,
    System
}

/// <summary>The authenticated caller for the current request/operation.</summary>
public sealed record CurrentUser(string UserId, string Email, IReadOnlyCollection<UserRole> Roles)
{
    public bool IsInRole(UserRole role) => Roles.Contains(role);
}

/// <summary>
/// Abstraction over "who is making this request." Infrastructure/Api implements this
/// (e.g. by reading JWT claims or, later, Entra claims) — Application only ever reads
/// from it, never decides how identity is established. That's what keeps Application
/// free of any Microsoft.Identity/JWT/ASP.NET Core reference.
/// </summary>
public interface ICurrentUserContext
{
    CurrentUser Current { get; }
}
