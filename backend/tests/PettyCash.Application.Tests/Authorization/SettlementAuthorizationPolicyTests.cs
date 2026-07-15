using PettyCash.Application.Abstractions;
using PettyCash.Application.Authorization;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;
using Xunit;

namespace PettyCash.Application.Tests.Authorization;

public class SettlementAuthorizationPolicyTests
{
    private readonly SettlementAuthorizationPolicy _policy = new();

    private static Settlement CreateSubmitted(string spenderId = "spender-1", string approverEmail = "manager@canex.com")
    {
        var settlement = Settlement.CreateDraft(spenderId, "Ahmed Ali", "W-001", approverEmail, new DateOnly(2026, 7, 1), "Purpose");
        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:IT", 100m, false, 14m, false, null, null);
        settlement.Submit();
        return settlement;
    }

    [Fact]
    public void EnsureCanCreate_Spender_DoesNotThrow()
    {
        var user = new CurrentUser("u1", "u1@canex.com", new[] { UserRole.Spender });
        var exception = Record.Exception(() => _policy.EnsureCanCreate(user));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(UserRole.Approver)]
    [InlineData(UserRole.ApAccountant)]
    [InlineData(UserRole.AppAdmin)]
    [InlineData(UserRole.System)]
    public void EnsureCanCreate_NonSpender_Throws(UserRole role)
    {
        var user = new CurrentUser("u1", "u1@canex.com", new[] { role });
        Assert.Throws<ForbiddenException>(() => _policy.EnsureCanCreate(user));
    }

    [Fact]
    public void EnsureCanEdit_Owner_InDraft_DoesNotThrow()
    {
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        var user = new CurrentUser("spender-1", "spender1@canex.com", new[] { UserRole.Spender });

        var exception = Record.Exception(() => _policy.EnsureCanEdit(settlement, user));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureCanEdit_NonOwner_Throws()
    {
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        var user = new CurrentUser("spender-2", "spender2@canex.com", new[] { UserRole.Spender });

        Assert.Throws<ForbiddenException>(() => _policy.EnsureCanEdit(settlement, user));
    }

    [Fact]
    public void EnsureCanEdit_OwnerButSubmitted_Throws()
    {
        var settlement = CreateSubmitted();
        var user = new CurrentUser("spender-1", "spender1@canex.com", new[] { UserRole.Spender });

        Assert.Throws<ForbiddenException>(() => _policy.EnsureCanEdit(settlement, user));
    }

    [Fact]
    public void EnsureCanApproveOrReject_AssignedApprover_DoesNotThrow()
    {
        var settlement = CreateSubmitted(approverEmail: "manager@canex.com");
        var user = new CurrentUser("m1", "manager@canex.com", new[] { UserRole.Approver });

        var exception = Record.Exception(() => _policy.EnsureCanApproveOrReject(settlement, user));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureCanApproveOrReject_UnassignedApprover_Throws()
    {
        var settlement = CreateSubmitted(approverEmail: "manager@canex.com");
        var user = new CurrentUser("m2", "other.manager@canex.com", new[] { UserRole.Approver });

        Assert.Throws<ForbiddenException>(() => _policy.EnsureCanApproveOrReject(settlement, user));
    }

    [Fact]
    public void EnsureCanApproveOrReject_SystemRole_BypassesOwnershipCheck()
    {
        var settlement = CreateSubmitted(approverEmail: "manager@canex.com");
        var user = new CurrentUser("system", "flow@canex.com", new[] { UserRole.System });

        var exception = Record.Exception(() => _policy.EnsureCanApproveOrReject(settlement, user));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureCanRecordJournal_SystemRole_DoesNotThrow()
    {
        var user = new CurrentUser("system", "flow@canex.com", new[] { UserRole.System });
        var exception = Record.Exception(() => _policy.EnsureCanRecordJournal(user));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(UserRole.Spender)]
    [InlineData(UserRole.Approver)]
    [InlineData(UserRole.ApAccountant)]
    [InlineData(UserRole.AppAdmin)]
    public void EnsureCanRecordJournal_NonSystemRole_Throws(UserRole role)
    {
        var user = new CurrentUser("u1", "u1@canex.com", new[] { role });
        Assert.Throws<ForbiddenException>(() => _policy.EnsureCanRecordJournal(user));
    }

    [Fact]
    public void EnsureCanView_ApAccountant_CanViewAnySettlement()
    {
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        var user = new CurrentUser("ap-1", "ap@canex.com", new[] { UserRole.ApAccountant });

        var exception = Record.Exception(() => _policy.EnsureCanView(settlement, user));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureCanView_UnrelatedSpender_Throws()
    {
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        var user = new CurrentUser("spender-2", "spender2@canex.com", new[] { UserRole.Spender });

        Assert.Throws<ForbiddenException>(() => _policy.EnsureCanView(settlement, user));
    }
}
