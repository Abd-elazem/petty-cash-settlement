using PettyCash.Application.Exceptions;
using PettyCash.Application.Settlements.Commands;
using PettyCash.Application.Tests.Fakes;
using PettyCash.Domain.Settlements;
using Xunit;

namespace PettyCash.Application.Tests.Settlements.Commands;

public class SubmitSettlementCommandHandlerTests
{
    private static async Task<(InMemorySettlementRepository repo, Settlement settlement)> SeedDraftWithLineAsync()
    {
        var repo = new InMemorySettlementRepository();
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:IT", 100m, false, 14m, false, null, null);
        await repo.AddAsync(settlement);
        return (repo, settlement);
    }

    [Fact]
    public async Task Handle_ValidDraft_MovesToSubmittedAndAudits()
    {
        var (repo, settlement) = await SeedDraftWithLineAsync();
        var audit = new FakeAuditLogger();
        var handler = new SubmitSettlementCommandHandler(repo, FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance, audit);

        var result = await handler.HandleAsync(new SubmitSettlementCommand(settlement.Id));

        Assert.Equal("Submitted", result.Status);
        Assert.False(result.IsEditable);
        Assert.Equal("Submitted", audit.Entries.Single().Action);
        Assert.Equal("Draft", audit.Entries.Single().FromStatus);
    }

    [Fact]
    public async Task Handle_NotOwner_ThrowsForbidden()
    {
        var (repo, settlement) = await SeedDraftWithLineAsync();
        var handler = new SubmitSettlementCommandHandler(repo, FakeCurrentUserContext.ForSpender("someone-else"), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(new SubmitSettlementCommand(settlement.Id)));
    }

    [Fact]
    public async Task Handle_EmptySettlement_ThrowsDomainValidationException()
    {
        var repo = new InMemorySettlementRepository();
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        await repo.AddAsync(settlement);
        var handler = new SubmitSettlementCommandHandler(repo, FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        await Assert.ThrowsAsync<PettyCash.Domain.Exceptions.DomainValidationException>(() =>
            handler.HandleAsync(new SubmitSettlementCommand(settlement.Id)));
    }
}
