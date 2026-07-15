using PettyCash.Application.Abstractions;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Tests.Fakes;

/// <summary>
/// In-memory fake, not a mock — no mocking library dependency. Good enough to exercise
/// real handler logic (including "not found" and basic persistence flow) without any
/// storage adapter.
/// </summary>
public sealed class InMemorySettlementRepository : ISettlementRepository
{
    private readonly Dictionary<Guid, Settlement> _store = new();

    public Task<Settlement?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(requestId, out var settlement);
        return Task.FromResult(settlement);
    }

    public Task<IReadOnlyList<Settlement>> GetBySpenderIdAsync(string spenderId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Settlement> result = _store.Values.Where(s => s.SpenderId == spenderId).ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        _store[settlement.Id] = settlement;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        if (!_store.ContainsKey(settlement.Id))
        {
            throw new ConcurrencyException(nameof(Settlement), settlement.Id);
        }

        _store[settlement.Id] = settlement;
        return Task.CompletedTask;
    }
}
