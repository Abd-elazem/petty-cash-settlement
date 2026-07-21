using Microsoft.EntityFrameworkCore;
using PettyCash.Application.Abstractions;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Infrastructure.Postgres.Repositories;

/// <summary>
/// Dev/local implementation of ISettlementRepository (DECISIONS.md D-002). Assumes a
/// scoped PettyCashDbContext per request/handler invocation — GetByIdAsync's result stays
/// tracked by the same context that a later UpdateAsync call uses, so mutations made via
/// Domain methods in between are already known to EF's change tracker.
///
/// Each method wraps its own explicit transaction. This gives atomicity across the
/// Settlement header + its owned Lines collection (the aggregate's own consistency
/// boundary) — it does NOT extend to IAuditLogger's separate SaveChangesAsync call in the
/// same handler; that cross-write gap is accepted and documented on IAuditLogger (D-015).
/// Deliberately not "fixed" here even though Postgres could technically support it: doing
/// so would make dev-only behavior stricter than production (SharePoint) can ever be,
/// which breaks "switching to SharePoint requires only a DI change" — the two adapters
/// must have the same consistency guarantees, not the strongest one either can offer.
/// </summary>
public sealed class PostgresSettlementRepository : ISettlementRepository
{
    private readonly PettyCashDbContext _db;

    public PostgresSettlementRepository(PettyCashDbContext db)
    {
        _db = db;
    }

    public async Task<Settlement?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _db.Settlements.FirstOrDefaultAsync(s => s.Id == requestId, cancellationToken);
    }

    public async Task<IReadOnlyList<Settlement>> GetBySpenderIdAsync(string spenderId, CancellationToken cancellationToken = default)
    {
        return await _db.Settlements
            .Where(s => s.SpenderId == spenderId)
            .OrderByDescending(s => s.SettlementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Settlement>> GetPendingApprovalByApproverEmailAsync(
        string approverEmail,
        CancellationToken cancellationToken = default)
    {
        return await _db.Settlements
            .Where(s => s.Status == SettlementStatus.Submitted)
            .Where(s => s.ApproverEmailSnapshot == approverEmail)
            .OrderByDescending(s => s.SettlementDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        _db.Settlements.Add(settlement);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (_db.Entry(settlement).State == EntityState.Detached)
            {
                _db.Settlements.Update(settlement);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConcurrencyException(nameof(Settlement), settlement.Id);
        }
    }
}
