using PettyCash.Application.Abstractions;

namespace PettyCash.Infrastructure.Postgres.Repositories;

public sealed class PostgresAuditLogger : IAuditLogger
{
    private readonly PettyCashDbContext _db;

    public PostgresAuditLogger(PettyCashDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
