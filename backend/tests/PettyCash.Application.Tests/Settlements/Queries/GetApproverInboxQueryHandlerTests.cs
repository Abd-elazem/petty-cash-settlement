using PettyCash.Application.Settlements.Queries;
using PettyCash.Application.Tests.Fakes;
using PettyCash.Domain.Settlements;
using Xunit;

namespace PettyCash.Application.Tests.Settlements.Queries;

public class GetApproverInboxQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlySubmittedSettlementsAssignedToCurrentApprover()
    {
        var repo = new InMemorySettlementRepository();

        var pendingForMe = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Pending mine");
        pendingForMe.AddLine("OFFICE_SUPPLIES", "6100", "Dept:Ops", 100m, false, 14m, false, null, null);
        pendingForMe.Submit();

        var draftForMe = Settlement.CreateDraft("spender-2", "Sara Adel", "W-002", "manager@canex.com", new DateOnly(2026, 7, 2), "Draft mine");

        var pendingForOther = Settlement.CreateDraft("spender-3", "Nour Ahmed", "W-003", "another.manager@canex.com", new DateOnly(2026, 7, 3), "Pending other");
        pendingForOther.AddLine("OFFICE_SUPPLIES", "6100", "Dept:Ops", 120m, false, 14m, false, null, null);
        pendingForOther.Submit();

        await repo.AddAsync(pendingForMe);
        await repo.AddAsync(draftForMe);
        await repo.AddAsync(pendingForOther);

        var handler = new GetApproverInboxQueryHandler(repo, FakeCurrentUserContext.ForApprover("manager@canex.com"));
        var result = await handler.HandleAsync(new GetApproverInboxQuery());

        Assert.Single(result);
        Assert.Equal("Pending mine", result[0].Purpose);
        Assert.Equal("Submitted", result[0].Status);
    }

    [Fact]
    public async Task Handle_NoPendingSettlementsForApprover_ReturnsEmptyList()
    {
        var repo = new InMemorySettlementRepository();
        var draftForMe = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Draft mine");
        await repo.AddAsync(draftForMe);

        var handler = new GetApproverInboxQueryHandler(repo, FakeCurrentUserContext.ForApprover("manager@canex.com"));
        var result = await handler.HandleAsync(new GetApproverInboxQuery());

        Assert.Empty(result);
    }
}
