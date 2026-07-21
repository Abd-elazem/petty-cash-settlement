namespace PettyCash.Infrastructure.SharePoint.Settlements.Models;

internal sealed record SharePointSettlementLineItem(
    string ItemId,
    string? ETag,
    Guid RequestId,
    Guid LineId,
    int LineNo,
    string CategoryCode,
    string ExpenseMainAccountSnapshot,
    string? DimensionDefaultsSnapshot,
    decimal GrossAmount,
    string Currency,
    bool IsVat,
    decimal VatAmount,
    decimal NetAmount,
    string? Notes,
    string? CarPlate,
    decimal? OdometerKm);
