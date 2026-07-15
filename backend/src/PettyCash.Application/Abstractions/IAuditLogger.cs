namespace PettyCash.Application.Abstractions;

/// <summary>One audit trail entry — see AuditLogEntry in ARCHITECTURE.md §4/§10 (A-013, additive, not in the original Guide schema).</summary>
public sealed record AuditLogEntry(
    Guid SettlementId,
    string Action,
    string PerformedByUserId,
    string? FromStatus,
    string? ToStatus,
    DateTime OccurredAtUtc,
    string? Details);

/// <summary>
/// Abstraction over wherever the audit trail is written (a new SharePoint list in
/// production, per A-013). Every command handler that changes settlement state calls
/// this — see ARCHITECTURE.md §10 ("every status transition writes an AuditLogEntry").
///
/// KNOWN LIMITATION (flagged at Milestone 0.3 self-review, not fixed here): handlers
/// call LogAsync AFTER the repository's UpdateAsync succeeds, with no shared transaction
/// between the two (see ISettlementRepository's doc comment on why there's no UnitOfWork).
/// If the state change persists but the audit write then fails, that transition goes
/// unlogged. Implementations of this interface should favor at-least-once delivery
/// (retry internally, or write to a durable queue) over silently swallowing failures.
/// An outbox pattern would close this gap properly but is more machinery than this
/// milestone's scope justifies — revisit if audit completeness becomes a hard
/// requirement rather than a best-effort one.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}
