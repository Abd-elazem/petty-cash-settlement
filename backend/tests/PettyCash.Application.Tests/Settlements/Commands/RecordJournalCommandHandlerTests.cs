using PettyCash.Application.Exceptions;
using PettyCash.Application.Settlements.Commands;
using PettyCash.Application.Tests.Fakes;
using PettyCash.Domain.Settlements;
using Xunit;

namespace PettyCash.Application.Tests.Settlements.Commands;

public class RecordJournalCommandHandlerTests
{
    private static async Task<(InMemorySettlementRepository repo, Settlement settlement)> SeedApprovedAsync()
    {
        var repo = new InMemorySettlementRepository();
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:IT", 100m, false, 14m, false, null, null);
        settlement.Submit();
        settlement.Approve();
        await repo.AddAsync(settlement);
        return (repo, settlement);
    }

    [Fact]
    public async Task Handle_FromApproved_RecordsJournalAndMovesToJournalled()
    {
        var (repo, settlement) = await SeedApprovedAsync();
        var handler = new RecordJournalCommandHandler(repo, FakeCurrentUserContext.ForSystem(), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        var result = await handler.HandleAsync(new RecordJournalCommand(settlement.Id, "PCASH-000123"));

        Assert.Equal("Journalled", result.Status);
        Assert.Equal("PCASH-000123", result.JournalBatchNumber);
    }

    [Fact]
    public async Task Handle_ByNonSystemRole_ThrowsForbidden()
    {
        var (repo, settlement) = await SeedApprovedAsync();
        var handler = new RecordJournalCommandHandler(repo, FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance, new FakeAuditLogger());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.HandleAsync(new RecordJournalCommand(settlement.Id, "PCASH-000123")));
    }

    [Fact]
    public async Task Handle_RetriedWithSameJournalNumber_IsIdempotent_DoesNotThrow()
    {
        // D-008: a retried Power Automate flow run must not fail or create a duplicate.
        var (repo, settlement) = await SeedApprovedAsync();
        var handler = new RecordJournalCommandHandler(repo, FakeCurrentUserContext.ForSystem(), TestAuthorizationPolicy.Instance, new FakeAuditLogger());
        await handler.HandleAsync(new RecordJournalCommand(settlement.Id, "PCASH-000123"));

        var result = await handler.HandleAsync(new RecordJournalCommand(settlement.Id, "PCASH-000123"));

        Assert.Equal("Journalled", result.Status);
        Assert.Equal("PCASH-000123", result.JournalBatchNumber);
    }

    [Fact]
    public async Task Handle_RetriedWithDifferentJournalNumber_Throws()
    {
        // A different journal number on an already-Journalled settlement is a real
        // conflict, not a safe retry — must NOT be swallowed by the idempotency check.
        var (repo, settlement) = await SeedApprovedAsync();
        var handler = new RecordJournalCommandHandler(repo, FakeCurrentUserContext.ForSystem(), TestAuthorizationPolicy.Instance, new FakeAuditLogger());
        await handler.HandleAsync(new RecordJournalCommand(settlement.Id, "PCASH-000123"));

        await Assert.ThrowsAsync<PettyCash.Domain.Exceptions.InvalidSettlementStateException>(() =>
            handler.HandleAsync(new RecordJournalCommand(settlement.Id, "PCASH-000999")));
    }
}
