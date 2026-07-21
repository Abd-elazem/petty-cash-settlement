using PettyCash.Infrastructure.SharePoint.Settlements.Models;

namespace PettyCash.Infrastructure.SharePoint.Settlements.Gateways;

internal interface ISharePointSettlementHeadersGateway
{
    Task<SharePointSettlementHeaderItem?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SharePointSettlementHeaderItem>> GetBySpenderIdAsync(string spenderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SharePointSettlementHeaderItem>> GetPendingApprovalByApproverEmailAsync(
        string approverEmail,
        CancellationToken cancellationToken = default);

    Task<SharePointSettlementHeaderItem> AddAsync(SharePointSettlementHeaderItem header, CancellationToken cancellationToken = default);

    Task<SharePointSettlementHeaderItem> UpdateAsync(
        SharePointSettlementHeaderItem header,
        string eTag,
        CancellationToken cancellationToken = default);
}
