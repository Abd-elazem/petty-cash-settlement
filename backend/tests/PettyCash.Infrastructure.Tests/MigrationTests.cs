using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PettyCash.Infrastructure.Tests;

/// <summary>
/// Sanity-checks that migrations were actually applied by PostgresContainerFixture
/// (deliverable: migration tests). The fixture itself already refuses to proceed with a
/// clear message if no migration files exist — this test additionally confirms the
/// expected core tables exist post-migration, catching a partially-applied or
/// misconfigured migration rather than just "did MigrateAsync not throw."
/// </summary>
[Collection("Postgres")]
public class MigrationTests
{
    private readonly PostgresContainerFixture _fixture;

    public MigrationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("Settlements")]
    [InlineData("SettlementLines")]
    [InlineData("CategoryMappings")]
    [InlineData("AppUserProfiles")]
    [InlineData("AuditLogs")]
    public async Task Migration_CreatesExpectedTable(string tableName)
    {
        await using var db = _fixture.CreateContext();

        var exists = await db.Database.SqlQueryRaw<int>(
            "SELECT 1 AS \"Value\" FROM information_schema.tables WHERE table_name = {0}", tableName)
            .AnyAsync();

        Assert.True(exists, $"Expected table '{tableName}' to exist after migration.");
    }
}
