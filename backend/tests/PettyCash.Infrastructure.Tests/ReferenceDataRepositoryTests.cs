using PettyCash.Infrastructure.Postgres.Repositories;
using Xunit;

namespace PettyCash.Infrastructure.Tests;

/// <summary>Seed-data verification (deliverable) plus basic repository contract checks for the two reference-data repositories.</summary>
[Collection("Postgres")]
public class ReferenceDataRepositoryTests
{
    private readonly PostgresContainerFixture _fixture;

    public ReferenceDataRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CategoryMappings_SeedData_ContainsFuelAndNonFuelCategories()
    {
        await using var db = _fixture.CreateContext();
        var repo = new PostgresCategoryMappingRepository(db);

        var fuel = await repo.GetByCategoryCodeAsync("FUEL");
        var supplies = await repo.GetByCategoryCodeAsync("OFFICE_SUPPLIES");

        Assert.NotNull(fuel);
        Assert.True(fuel!.KmRequired);
        Assert.NotNull(supplies);
        Assert.False(supplies!.KmRequired);
    }

    [Fact]
    public async Task CategoryMappings_GetByCode_UnknownCode_ReturnsNull()
    {
        await using var db = _fixture.CreateContext();
        var repo = new PostgresCategoryMappingRepository(db);

        var result = await repo.GetByCategoryCodeAsync("DOES_NOT_EXIST");

        Assert.Null(result);
    }

    [Fact]
    public async Task CategoryMappings_GetAllActive_OnlyReturnsActiveRows()
    {
        await using var db = _fixture.CreateContext();
        var repo = new PostgresCategoryMappingRepository(db);

        var active = await repo.GetAllActiveAsync();

        Assert.True(active.Count >= 3); // the three seeded categories
        Assert.All(active, m => Assert.True(m.Active));
    }

    [Fact]
    public async Task AppUserProfiles_SeedData_ContainsDemoSpender()
    {
        await using var db = _fixture.CreateContext();
        var repo = new PostgresAppUserProfileRepository(db);

        var profile = await repo.GetByAppUserIdAsync("spender.demo");

        Assert.NotNull(profile);
        Assert.True(profile!.Active);
        Assert.Equal("manager.demo@canex.com", profile.ApproverEmail);
    }

    [Fact]
    public async Task AppUserProfiles_GetByAppUserId_UnknownId_ReturnsNull()
    {
        await using var db = _fixture.CreateContext();
        var repo = new PostgresAppUserProfileRepository(db);

        var result = await repo.GetByAppUserIdAsync("does-not-exist");

        Assert.Null(result);
    }
}
