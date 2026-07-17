namespace PettyCash.Infrastructure.SharePoint.Audit.Models;

internal sealed record SharePointAuditLogItem(
    Guid SettlementId,
    string Action,
    string PerformedByUserId,
    string? FromStatus,
    string? ToStatus,
    DateTime OccurredAtUtc,
    string? Details);
