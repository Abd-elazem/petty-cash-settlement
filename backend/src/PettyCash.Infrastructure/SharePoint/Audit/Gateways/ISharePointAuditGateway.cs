using PettyCash.Infrastructure.SharePoint.Audit.Models;

namespace PettyCash.Infrastructure.SharePoint.Audit.Gateways;

internal interface ISharePointAuditGateway
{
    Task AddAsync(SharePointAuditLogItem entry, CancellationToken cancellationToken = default);
}
