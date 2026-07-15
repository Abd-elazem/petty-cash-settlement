using PettyCash.Domain.Settlements;
using PettyCash.Infrastructure.Postgres.Repositories;
using Xunit;

namespace PettyCash.Infrastructure.Tests;

/// <summary>
/// Round-trip and aggregate persistence tests against real Postgres (Testcontainers,
/// no mocks). Each test uses a fresh Guid-based SpenderId to stay independent despite
/// sharing one container/database across the whole test run (a full reset per test would
/// be slower and isn't needed here).
/// </summary>
[Collection("Postgres")]
public class SettlementRepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public SettlementRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddThenGetById_RoundTripsHeaderFieldsExactly()
    {
        await using var db = _fixture.CreateContext();
        var repo = new PostgresSettlementRepository(db);
        var settlement = Settlement.CreateDraft(
            $"spender-{Guid.NewGuid():N}", "Ahmed Ali", "W-001", "manager@canex.com",
            new DateOnly(2026, 7, 1), "Site visit expenses");

        await repo.AddAsync(settlement);

        await using var readDb = _fixture.CreateContext();
        var reloaded = await new PostgresSettlementRepository(readDb).GetByIdAsync(settlement.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(settlement.SpenderId, reloaded!.SpenderId);
        Assert.Equal("Ahmed Ali", reloaded.SpenderNameSnapshot);
        Assert.Equal("manager@canex.com", reloaded.ApproverEmailSnapshot);
        Assert.Equal(SettlementStatus.Draft, reloaded.Status);
        Assert.Equal(1, reloaded.Version);
    }

    [Fact]
    public async Task AddWithLines_RoundTripsFullLineGraph_IncludingVatAndOdometer()
    {
        await using var db = _fixture.CreateContext();
        var repo = new PostgresSettlementRepository(db);
        var settlement = Settlement.CreateDraft(
            $"spender-{Guid.NewGuid():N}", "Sara Adel", "W-002", "manager@canex.com",
            new DateOnly(2026, 7, 1), "Fuel + supplies");

        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:IT", 150m, isVat: true, vatRatePercent: 14m,
            kmRequired: false, odometer: null, notes: "Printer paper");
        settlement.AddLine("FUEL", "6200", "Dept:Fleet", 500m, isVat: false, vatRatePercent: 14m,
            kmRequired: true, odometer: new OdometerReading("ABC-1234", 45210m), notes: null);

        await repo.AddAsync(settlement);

        await using var readDb = _fixture.CreateContext();
        var reloaded = await new PostgresSettlementRepository(readDb).GetByIdAsync(settlement.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(2, reloaded!.Lines.Count);
        Assert.Equal(650m, reloaded.TotalAmount);

        var suppliesLine = reloaded.Lines.Single(l => l.CategoryCode == "OFFICE_SUPPLIES");
        Assert.True(suppliesLine.IsVat);
        Assert.Equal(18.42m, suppliesLine.VatBreakdown.Vat); // round(150 * 14/114, 2)
        Assert.Null(suppliesLine.Odometer);

        var fuelLine = reloaded.Lines.Single(l => l.CategoryCode == "FUEL");
        Assert.False(fuelLine.IsVat);
        Assert.NotNull(fuelLine.Odometer);
        Assert.Equal("ABC-1234", fuelLine.Odometer!.CarPlate);
        Assert.Equal(45210m, fuelLine.Odometer.OdometerKm);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        await using var db = _fixture.CreateContext();
        var repo = new PostgresSettlementRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySpenderId_ReturnsOnlyThatSpendersSettlements()
    {
        var spenderId = $"spender-{Guid.NewGuid():N}";
        await using var db = _fixture.CreateContext();
        var repo = new PostgresSettlementRepository(db);
        var mine = Settlement.CreateDraft(spenderId, "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Mine");
        var someoneElses = Settlement.CreateDraft($"spender-{Guid.NewGuid():N}", "Sara Adel", "W-002", "manager@canex.com", new DateOnly(2026, 7, 1), "Not mine");
        await repo.AddAsync(mine);
        await repo.AddAsync(someoneElses);

        await using var readDb = _fixture.CreateContext();
        var result = await new PostgresSettlementRepository(readDb).GetBySpenderIdAsync(spenderId);

        Assert.Single(result);
        Assert.Equal("Mine", result[0].Purpose);
    }

    [Fact]
    public async Task Update_PersistsStatusTransitionAndLineChanges()
    {
        await using var db = _fixture.CreateContext();
        var repo = new PostgresSettlementRepository(db);
        var settlement = Settlement.CreateDraft(
            $"spender-{Guid.NewGuid():N}", "Ahmed Ali", "W-001", "manager@canex.com",
            new DateOnly(2026, 7, 1), "Purpose");
        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:IT", 100m, false, 14m, false, null, null);
        await repo.AddAsync(settlement);

        settlement.Submit();
        await repo.UpdateAsync(settlement);

        await using var readDb = _fixture.CreateContext();
        var reloaded = await new PostgresSettlementRepository(readDb).GetByIdAsync(settlement.Id);

        Assert.Equal(SettlementStatus.Submitted, reloaded!.Status);
    }

    [Fact]
    public async Task Update_RejectThenReopen_PersistsVersionIncrement()
    {
        await using var db = _fixture.CreateContext();
        var repo = new PostgresSettlementRepository(db);
        var settlement = Settlement.CreateDraft(
            $"spender-{Guid.NewGuid():N}", "Ahmed Ali", "W-001", "manager@canex.com",
            new DateOnly(2026, 7, 1), "Purpose");
        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:IT", 100m, false, 14m, false, null, null);
        settlement.Submit();
        await repo.AddAsync(settlement);

        settlement.Reject("Missing receipt");
        await repo.UpdateAsync(settlement);
        settlement.ReopenForEdit();
        await repo.UpdateAsync(settlement);

        await using var readDb = _fixture.CreateContext();
        var reloaded = await new PostgresSettlementRepository(readDb).GetByIdAsync(settlement.Id);

        Assert.Equal(SettlementStatus.Draft, reloaded!.Status);
        Assert.Equal(2, reloaded.Version);
    }

    [Fact]
    public async Task Update_RemovedLine_IsNotReloaded()
    {
        await using var db = _fixture.CreateContext();
        var repo = new PostgresSettlementRepository(db);
        var settlement = Settlement.CreateDraft(
            $"spender-{Guid.NewGuid():N}", "Ahmed Ali", "W-001", "manager@canex.com",
            new DateOnly(2026, 7, 1), "Purpose");
        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:IT", 100m, false, 14m, false, null, null);
        var secondLine = settlement.AddLine("GOVERNMENT_FEES", "6300", "Dept:Legal", 50m, false, 14m, false, null, null);
        await repo.AddAsync(settlement);

        settlement.RemoveLine(secondLine.LineId);
        await repo.UpdateAsync(settlement);

        await using var readDb = _fixture.CreateContext();
        var reloaded = await new PostgresSettlementRepository(readDb).GetByIdAsync(settlement.Id);

        Assert.Single(reloaded!.Lines);
        Assert.Equal("OFFICE_SUPPLIES", reloaded.Lines[0].CategoryCode);
    }
}
