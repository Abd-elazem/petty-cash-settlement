using PettyCash.Application.Abstractions;
using PettyCash.Infrastructure.Postgres.Repositories;
using Xunit;

namespace PettyCash.Infrastructure.Tests;

[Collection("Postgres")]
public class AuditLoggerTests
{
    private readonly PostgresContainerFixture _fixture;

    public AuditLoggerTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LogAsync_PersistsEntry_RetrievableViaDirectQuery()
    {
        await using var db = _fixture.CreateContext();
        var logger = new PostgresAuditLogger(db);
        var settlementId = Guid.NewGuid();
        var entry = new AuditLogEntry(settlementId, "Submitted", "spender-1", "Draft", "Submitted", DateTime.UtcNow, null);

        await logger.LogAsync(entry);

        await using var readDb = _fixture.CreateContext();
        var stored = readDb.AuditLogs.SingleOrDefault(a => a.SettlementId == settlementId);
        Assert.NotNull(stored);
        Assert.Equal("Submitted", stored!.Action);
    }
}
