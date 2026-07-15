using Microsoft.EntityFrameworkCore;
using PettyCash.Application.Abstractions;

namespace PettyCash.Infrastructure.Postgres.Repositories;

public sealed class PostgresAppUserProfileRepository : IAppUserProfileRepository
{
    private readonly PettyCashDbContext _db;

    public PostgresAppUserProfileRepository(PettyCashDbContext db)
    {
        _db = db;
    }

    public async Task<AppUserProfileReadModel?> GetByAppUserIdAsync(string appUserId, CancellationToken cancellationToken = default)
    {
        return await _db.AppUserProfiles.FirstOrDefaultAsync(u => u.AppUserId == appUserId, cancellationToken);
    }
}
