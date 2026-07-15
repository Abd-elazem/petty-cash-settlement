using PettyCash.Application.Abstractions;

namespace PettyCash.Application.Tests.Fakes;

public sealed class FakeAuditLogger : IAuditLogger
{
    public List<AuditLogEntry> Entries { get; } = new();

    public Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}
