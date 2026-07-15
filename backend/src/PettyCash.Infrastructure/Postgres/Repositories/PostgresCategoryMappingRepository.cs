using Microsoft.EntityFrameworkCore;
using PettyCash.Application.Abstractions;

namespace PettyCash.Infrastructure.Postgres.Repositories;

public sealed class PostgresCategoryMappingRepository : ICategoryMappingRepository
{
    private readonly PettyCashDbContext _db;

    public PostgresCategoryMappingRepository(PettyCashDbContext db)
    {
        _db = db;
    }

    public async Task<CategoryMappingReadModel?> GetByCategoryCodeAsync(string categoryCode, CancellationToken cancellationToken = default)
    {
        return await _db.CategoryMappings.FirstOrDefaultAsync(c => c.CategoryCode == categoryCode, cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryMappingReadModel>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CategoryMappings.Where(c => c.Active).ToListAsync(cancellationToken);
    }
}
