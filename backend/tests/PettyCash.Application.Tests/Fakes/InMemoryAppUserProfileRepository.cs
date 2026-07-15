using PettyCash.Application.Abstractions;

namespace PettyCash.Application.Tests.Fakes;

public sealed class InMemoryAppUserProfileRepository : IAppUserProfileRepository
{
    private readonly Dictionary<string, AppUserProfileReadModel> _store = new();

    public InMemoryAppUserProfileRepository Seed(AppUserProfileReadModel profile)
    {
        _store[profile.AppUserId] = profile;
        return this;
    }

    public Task<AppUserProfileReadModel?> GetByAppUserIdAsync(string appUserId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(appUserId, out var profile);
        return Task.FromResult(profile);
    }
}
