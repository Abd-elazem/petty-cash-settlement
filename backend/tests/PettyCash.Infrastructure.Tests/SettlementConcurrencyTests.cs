using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;
using PettyCash.Infrastructure.Postgres.Repositories;
using Xunit;

namespace PettyCash.Infrastructure.Tests;

/// <summary>Proves the xmin-based optimistic concurrency configured in SettlementConfiguration actually fires and maps to Application's ConcurrencyException (not a raw EF/Npgsql exception leaking out of Infrastructure).</summary>
[Collection("Postgres")]
public class SettlementConcurrencyTests
{
    private readonly PostgresContainerFixture _fixture;

    public SettlementConcurrencyTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConcurrentUpdates_SecondWriterGetsConcurrencyException()
    {
        var settlementId = await SeedSubmittedSettlementAsync();

        await using var dbA = _fixture.CreateContext();
        await using var dbB = _fixture.CreateContext();
        var repoA = new PostgresSettlementRepository(dbA);
        var repoB = new PostgresSettlementRepository(dbB);

        var settlementA = await repoA.GetByIdAsync(settlementId);
        var settlementB = await repoB.GetByIdAsync(settlementId);

        settlementA!.Approve();
        await repoA.UpdateAsync(settlementA); // succeeds, advances the row's xmin

        settlementB!.Reject("Stale view — should conflict");
        await Assert.ThrowsAsync<ConcurrencyException>(() => repoB.UpdateAsync(settlementB));
    }

    private async Task<Guid> SeedSubmittedSettlementAsync()
    {
        await using var db = _fixture.CreateContext();
        var repo = new PostgresSettlementRepository(db);
        var settlement = Settlement.CreateDraft(
            $"spender-{Guid.NewGuid():N}", "Ahmed Ali", "W-001", "manager@canex.com",
            new DateOnly(2026, 7, 1), "Purpose");
        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:IT", 100m, false, 14m, false, null, null);
        settlement.Submit();
        await repo.AddAsync(settlement);
        return settlement.Id;
    }
}
