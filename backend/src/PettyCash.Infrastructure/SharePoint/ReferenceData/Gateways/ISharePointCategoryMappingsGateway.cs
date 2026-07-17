using PettyCash.Infrastructure.SharePoint.ReferenceData.Models;

namespace PettyCash.Infrastructure.SharePoint.ReferenceData.Gateways;

internal interface ISharePointCategoryMappingsGateway
{
    Task<SharePointCategoryMappingItem?> GetByCategoryCodeAsync(string categoryCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SharePointCategoryMappingItem>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
