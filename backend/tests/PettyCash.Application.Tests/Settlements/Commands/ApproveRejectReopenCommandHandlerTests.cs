using PettyCash.Application.Exceptions;
using PettyCash.Application.Settlements.Commands;
using PettyCash.Application.Tests.Fakes;
using PettyCash.Domain.Settlements;
using Xunit;

namespace PettyCash.Application.Tests.Settlements.Commands;

public class ApproveRejectReopenCommandHandlerTests
{
    private static async Task<(InMemorySettlementRepository repo, Settlement settlement)> SeedSubmittedAsync()
    {
        var repo = new InMemorySettlementRepository();
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:IT", 100m, false, 14m, false, null, null);
        settlement.Submit();
        await repo.AddAsync(settlement);
        return (repo, settlement);
    }

    [Fact]
    public async Task Approve_ByAssignedApprover_Succeeds()
    {
        var (repo, settlement) = await SeedSubmittedAsync();
        var handler = new ApproveSettlementCommandHandler(repo, FakeCurrentUserContext.ForApprover("manager@canex.com"), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        var result = await handler.HandleAsync(new ApproveSettlementCommand(settlement.Id));

        Assert.Equal("Approved", result.Status);
    }

    [Fact]
    public async Task Approve_ByDifferentApprover_ThrowsForbidden()
    {
        var (repo, settlement) = await SeedSubmittedAsync();
        var handler = new ApproveSettlementCommandHandler(repo, FakeCurrentUserContext.ForApprover("someone.else@canex.com"), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(new ApproveSettlementCommand(settlement.Id)));
    }

    [Fact]
    public async Task Approve_BySystemRole_Succeeds()
    {
        // Power Automate calling back into the API — D-006.
        var (repo, settlement) = await SeedSubmittedAsync();
        var handler = new ApproveSettlementCommandHandler(repo, FakeCurrentUserContext.ForSystem(), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        var result = await handler.HandleAsync(new ApproveSettlementCommand(settlement.Id));

        Assert.Equal("Approved", result.Status);
    }

    [Fact]
    public async Task Reject_ByAssignedApprover_MovesToRejected_NotDraft()
    {
        var (repo, settlement) = await SeedSubmittedAsync();
        var handler = new RejectSettlementCommandHandler(repo, FakeCurrentUserContext.ForApprover("manager@canex.com"), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        var result = await handler.HandleAsync(new RejectSettlementCommand(settlement.Id, "Missing receipt"));

        Assert.Equal("Rejected", result.Status);
        Assert.Equal("Missing receipt", result.ApprovalComment);
        Assert.False(result.IsEditable);
    }

    [Fact]
    public async Task Reopen_ByOwner_ReturnsToDraft_AndIsEditableAgain()
    {
        var (repo, settlement) = await SeedSubmittedAsync();
        settlement.Reject("Wrong category");
        await repo.UpdateAsync(settlement);

        var handler = new ReopenSettlementCommandHandler(repo, FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        var result = await handler.HandleAsync(new ReopenSettlementCommand(settlement.Id));

        Assert.Equal("Draft", result.Status);
        Assert.True(result.IsEditable);
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task Reopen_ByNonOwner_ThrowsForbidden()
    {
        var (repo, settlement) = await SeedSubmittedAsync();
        settlement.Reject("Wrong category");
        await repo.UpdateAsync(settlement);

        var handler = new ReopenSettlementCommandHandler(repo, FakeCurrentUserContext.ForSpender("someone-else"), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(new ReopenSettlementCommand(settlement.Id)));
    }
}
