namespace PettyCash.Application.Abstractions;

public interface ICategoryMappingRepository
{
    Task<CategoryMappingReadModel?> GetByCategoryCodeAsync(string categoryCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryMappingReadModel>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
