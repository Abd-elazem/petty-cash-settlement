using PettyCash.Application.Abstractions;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Authorization;

/// <summary>
/// The single place ownership and role rules are enforced, per ARCHITECTURE.md §7.
/// Command handlers call these instead of inlining role checks, so "can this user do
/// this" only has one implementation to test and change.
/// </summary>
public interface ISettlementAuthorizationPolicy
{
    void EnsureCanCreate(CurrentUser user);
    void EnsureCanEdit(Settlement settlement, CurrentUser user);
    void EnsureCanSubmit(Settlement settlement, CurrentUser user);
    void EnsureCanApproveOrReject(Settlement settlement, CurrentUser user);
    void EnsureCanReopen(Settlement settlement, CurrentUser user);
    void EnsureCanRecordJournal(CurrentUser user);
    void EnsureCanView(Settlement settlement, CurrentUser user);
}

public sealed class SettlementAuthorizationPolicy : ISettlementAuthorizationPolicy
{
    public void EnsureCanCreate(CurrentUser user)
    {
        if (!user.IsInRole(UserRole.Spender))
        {
            throw new ForbiddenException("Only spenders can create settlements.");
        }
    }

    public void EnsureCanEdit(Settlement settlement, CurrentUser user)
    {
        EnsureOwner(settlement, user);

        // Belt-and-braces: Settlement.AddLine/UpdateLine/RemoveLine already throw
        // InvalidSettlementStateException outside Draft. Checking IsEditable here too
        // means a non-owner-shaped bug fails with a clear 403 instead of surfacing a
        // Domain exception the caller has to know to interpret as "not authorized."
        if (!settlement.IsEditable)
        {
            throw new ForbiddenException($"Settlement is not editable in its current status ({settlement.Status}).");
        }
    }

    public void EnsureCanSubmit(Settlement settlement, CurrentUser user) => EnsureOwner(settlement, user);

    public void EnsureCanApproveOrReject(Settlement settlement, CurrentUser user)
    {
        // Power Automate calling back into the API after a Teams/Outlook approval — D-006.
        if (user.IsInRole(UserRole.System))
        {
            return;
        }

        if (!user.IsInRole(UserRole.Approver))
        {
            throw new ForbiddenException("Only the assigned approver can approve or reject this settlement.");
        }

        if (!string.Equals(settlement.ApproverEmailSnapshot, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("You are not the approver assigned to this settlement.");
        }
    }

    public void EnsureCanReopen(Settlement settlement, CurrentUser user) => EnsureOwner(settlement, user);

    public void EnsureCanRecordJournal(CurrentUser user)
    {
        if (!user.IsInRole(UserRole.System))
        {
            throw new ForbiddenException("Only the automated journal-creation process may record a journal.");
        }
    }

    public void EnsureCanView(Settlement settlement, CurrentUser user)
    {
        if (IsOwner(settlement, user)) return;
        if (user.IsInRole(UserRole.Approver) &&
            string.Equals(settlement.ApproverEmailSnapshot, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        if (user.IsInRole(UserRole.ApAccountant)) return;
        if (user.IsInRole(UserRole.AppAdmin)) return;

        throw new ForbiddenException("You do not have access to this settlement.");
    }

    private static void EnsureOwner(Settlement settlement, CurrentUser user)
    {
        if (!IsOwner(settlement, user))
        {
            throw new ForbiddenException("You can only modify your own settlements.");
        }
    }

    private static bool IsOwner(Settlement settlement, CurrentUser user) => settlement.SpenderId == user.UserId;
}
