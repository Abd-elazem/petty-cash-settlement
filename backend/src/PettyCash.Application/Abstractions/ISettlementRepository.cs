using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Abstractions;

/// <summary>
/// Persistence boundary for the Settlement aggregate. Two implementations will exist:
/// a Postgres one (dev) and a SharePoint one (production) — both satisfying this exact
/// contract, per DECISIONS.md D-002. No EF/Npgsql/Graph types appear anywhere near this
/// interface; that's the whole point of it living in Application.
///
/// No IUnitOfWork/SaveChanges here on purpose: SharePoint has no cross-list transaction
/// primitive (Header + Lines are separate lists, per Guide §5.5), so a UnitOfWork
/// abstraction would imply an atomicity guarantee the production adapter can't actually
/// give. AddAsync/UpdateAsync are expected to persist immediately; each adapter is
/// responsible for making its own writes as atomic as its backing store allows.
/// This is a known limitation, not an oversight — see the original risk list
/// ("no idempotency guarantee on journal creation") which RecordJournalCommand
/// addresses at the Application level instead (D-008).
/// </summary>
public interface ISettlementRepository
{
    Task<Settlement?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Settlement>> GetBySpenderIdAsync(string spenderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Settlement>> GetPendingApprovalByApproverEmailAsync(
        string approverEmail,
        CancellationToken cancellationToken = default);

    Task AddAsync(Settlement settlement, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes made to an already-loaded aggregate. Implementations should use
    /// the aggregate's Version for optimistic concurrency and throw
    /// PettyCash.Application.Exceptions.ConcurrencyException on conflict.
    /// </summary>
    Task UpdateAsync(Settlement settlement, CancellationToken cancellationToken = default);
}
