using PettyCash.Application.Abstractions;
using PettyCash.Infrastructure.SharePoint.Audit.Gateways;
using PettyCash.Infrastructure.SharePoint.Audit.Models;

namespace PettyCash.Infrastructure.SharePoint.Repositories;

internal sealed class SharePointAuditLogger : IAuditLogger
{
    private readonly ISharePointAuditGateway _gateway;

    internal SharePointAuditLogger(ISharePointAuditGateway gateway)
    {
        _gateway = gateway;
    }

    public Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        var item = new SharePointAuditLogItem(
            SettlementId: entry.SettlementId,
            Action: entry.Action,
            PerformedByUserId: entry.PerformedByUserId,
            FromStatus: entry.FromStatus,
            ToStatus: entry.ToStatus,
            OccurredAtUtc: entry.OccurredAtUtc,
            Details: entry.Details);

        return _gateway.AddAsync(item, cancellationToken);
    }
}
