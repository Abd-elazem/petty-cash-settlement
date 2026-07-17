namespace PettyCash.Infrastructure.SharePoint.Settlements.Models;

internal sealed record SharePointSettlementHeaderItem(
    string ItemId,
    string? ETag,
    Guid RequestId,
    int Version,
    DateOnly SettlementDate,
    string Purpose,
    string SpenderId,
    string SpenderNameSnapshot,
    string WorkerIdSnapshot,
    string ApproverEmailSnapshot,
    string Status,
    string? ApprovalComment,
    string? JournalBatchNumber);
