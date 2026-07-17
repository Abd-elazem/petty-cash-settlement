using PettyCash.Infrastructure.SharePoint.Settlements.Models;

namespace PettyCash.Infrastructure.SharePoint.Settlements.Gateways;

internal interface ISharePointSettlementLinesGateway
{
    Task<IReadOnlyList<SharePointSettlementLineItem>> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task ReplaceForSettlementAsync(
        Guid requestId,
        IReadOnlyList<SharePointSettlementLineItem> lines,
        CancellationToken cancellationToken = default);
}
