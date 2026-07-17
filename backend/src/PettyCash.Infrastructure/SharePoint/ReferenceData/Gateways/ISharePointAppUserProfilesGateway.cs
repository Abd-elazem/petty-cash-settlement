using PettyCash.Infrastructure.SharePoint.ReferenceData.Models;

namespace PettyCash.Infrastructure.SharePoint.ReferenceData.Gateways;

internal interface ISharePointAppUserProfilesGateway
{
    Task<SharePointAppUserProfileItem?> GetByAppUserIdAsync(string appUserId, CancellationToken cancellationToken = default);
}
