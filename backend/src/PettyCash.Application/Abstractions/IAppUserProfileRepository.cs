namespace PettyCash.Application.Abstractions;

public interface IAppUserProfileRepository
{
    Task<AppUserProfileReadModel?> GetByAppUserIdAsync(string appUserId, CancellationToken cancellationToken = default);
}
