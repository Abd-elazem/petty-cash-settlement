using PettyCash.Application.Abstractions;
using PettyCash.Infrastructure.SharePoint.ReferenceData.Gateways;

namespace PettyCash.Infrastructure.SharePoint.Repositories;

internal sealed class SharePointAppUserProfileRepository : IAppUserProfileRepository
{
    private readonly ISharePointAppUserProfilesGateway _gateway;

    internal SharePointAppUserProfileRepository(ISharePointAppUserProfilesGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<AppUserProfileReadModel?> GetByAppUserIdAsync(string appUserId, CancellationToken cancellationToken = default)
    {
        var item = await _gateway.GetByAppUserIdAsync(appUserId, cancellationToken);
        return item is null
            ? null
            : new AppUserProfileReadModel(
                item.AppUserId,
                item.DisplayName,
                item.WorkerId,
                item.ApproverEmail,
                item.Active);
    }
}
