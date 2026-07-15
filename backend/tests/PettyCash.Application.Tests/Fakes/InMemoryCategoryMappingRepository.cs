using PettyCash.Application.Abstractions;

namespace PettyCash.Application.Tests.Fakes;

public sealed class InMemoryCategoryMappingRepository : ICategoryMappingRepository
{
    private readonly Dictionary<string, CategoryMappingReadModel> _store = new();

    public InMemoryCategoryMappingRepository Seed(CategoryMappingReadModel mapping)
    {
        _store[mapping.CategoryCode] = mapping;
        return this;
    }

    public Task<CategoryMappingReadModel?> GetByCategoryCodeAsync(string categoryCode, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(categoryCode, out var mapping);
        return Task.FromResult(mapping);
    }

    public Task<IReadOnlyList<CategoryMappingReadModel>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CategoryMappingReadModel> result = _store.Values.Where(m => m.Active).ToList();
        return Task.FromResult(result);
    }
}
