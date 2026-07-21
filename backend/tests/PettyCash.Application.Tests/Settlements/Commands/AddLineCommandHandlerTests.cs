using PettyCash.Application.Abstractions;
using PettyCash.Application.Exceptions;
using PettyCash.Application.Settlements.Commands;
using PettyCash.Application.Tests.Fakes;
using PettyCash.Domain.Settlements;
using Xunit;

namespace PettyCash.Application.Tests.Settlements.Commands;

public class AddLineCommandHandlerTests
{
    private static async Task<(InMemorySettlementRepository repo, Settlement settlement)> SeedDraftAsync()
    {
        var repo = new InMemorySettlementRepository();
        var settlement = Settlement.CreateDraft("spender-1", "Ahmed Ali", "W-001", "manager@canex.com", new DateOnly(2026, 7, 1), "Purpose");
        await repo.AddAsync(settlement);
        return (repo, settlement);
    }

    private static InMemoryCategoryMappingRepository SeedMappings() => new InMemoryCategoryMappingRepository()
        .Seed(new("OFFICE_SUPPLIES", "Office Supplies", "6100", "Dept:IT", null, null, KmRequired: false, Active: true))
        .Seed(new("FUEL", "Fuel", "6200", "Dept:Fleet", null, null, KmRequired: true, Active: true))
        .Seed(new("DISCONTINUED", "Discontinued", "6300", "Dept:X", null, null, KmRequired: false, Active: false));

    [Fact]
    public async Task Handle_ValidLine_AddsLineAndReturnsUpdatedTotal()
    {
        var (repo, settlement) = await SeedDraftAsync();
        var handler = new AddLineCommandHandler(
            repo, SeedMappings(), new FakeVatConfiguration(), FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance);

        var result = await handler.HandleAsync(new AddLineCommand(settlement.Id, "OFFICE_SUPPLIES", 150m, false, "Paper", null, null));

        Assert.Single(result.Lines);
        Assert.Equal(150m, result.TotalAmount);
    }

    [Fact]
    public async Task Handle_FuelCategoryWithoutOdometer_ThrowsDomainException()
    {
        var (repo, settlement) = await SeedDraftAsync();
        var handler = new AddLineCommandHandler(
            repo, SeedMappings(), new FakeVatConfiguration(), FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance);

        await Assert.ThrowsAsync<PettyCash.Domain.Exceptions.DomainValidationException>(() =>
            handler.HandleAsync(new AddLineCommand(settlement.Id, "FUEL", 500m, false, null, null, null)));
    }

    [Fact]
    public async Task Handle_InactiveCategory_ThrowsNotFound()
    {
        var (repo, settlement) = await SeedDraftAsync();
        var handler = new AddLineCommandHandler(
            repo, SeedMappings(), new FakeVatConfiguration(), FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(new AddLineCommand(settlement.Id, "DISCONTINUED", 100m, false, null, null, null)));
    }

    [Fact]
    public async Task Handle_ForSomeoneElsesSettlement_ThrowsForbidden()
    {
        var (repo, settlement) = await SeedDraftAsync(); // owned by spender-1
        var handler = new AddLineCommandHandler(
            repo, SeedMappings(), new FakeVatConfiguration(), FakeCurrentUserContext.ForSpender("spender-2"), TestAuthorizationPolicy.Instance);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.HandleAsync(new AddLineCommand(settlement.Id, "OFFICE_SUPPLIES", 100m, false, null, null, null)));
    }

    [Fact]
    public async Task Handle_AfterSubmit_ThrowsForbidden_NotRawDomainException()
    {
        // EnsureCanEdit is expected to translate "not editable" into a clean 403-shaped
        // ForbiddenException rather than letting InvalidSettlementStateException leak out.
        var (repo, settlement) = await SeedDraftAsync();
        settlement.AddLine("OFFICE_SUPPLIES", "6100", "Dept:IT", 50m, false, 14m, false, null, null);
        settlement.Submit();
        await repo.UpdateAsync(settlement);

        var handler = new AddLineCommandHandler(
            repo, SeedMappings(), new FakeVatConfiguration(), FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.HandleAsync(new AddLineCommand(settlement.Id, "OFFICE_SUPPLIES", 100m, false, null, null, null)));
    }

    [Fact]
    public async Task Handle_UnknownSettlement_ThrowsNotFound()
    {
        var handler = new AddLineCommandHandler(
            new InMemorySettlementRepository(), SeedMappings(), new FakeVatConfiguration(), FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(new AddLineCommand(Guid.NewGuid(), "OFFICE_SUPPLIES", 100m, false, null, null, null)));
    }

    [Fact]
    public async Task Handle_CategoryWithNullDimensionDefaults_LineSnapshotIsNull()
    {
        // Arrange: mapping with genuinely null DimensionDefaults (no financial dimension defaults configured)
        var mappings = new InMemoryCategoryMappingRepository()
            .Seed(new("OFFICE_SUPPLIES", "Office Supplies", "6100",
                      DimensionDefaults: null, null, null,
                      KmRequired: false, Active: true));
        var (repo, settlement) = await SeedDraftAsync();
        var handler = new AddLineCommandHandler(
            repo, mappings, new FakeVatConfiguration(),
            FakeCurrentUserContext.ForSpender(), TestAuthorizationPolicy.Instance);

        // Act
        await handler.HandleAsync(
            new AddLineCommand(settlement.Id, "OFFICE_SUPPLIES", 100m, false, null, null, null));

        // Assert: null flows through handler → Domain → in-memory repo without coercion
        var reloaded = await repo.GetByIdAsync(settlement.Id);
        Assert.Single(reloaded!.Lines);
        Assert.Null(reloaded.Lines[0].DimensionDefaultsSnapshot);
    }
}
