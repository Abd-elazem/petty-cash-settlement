namespace PettyCash.Application.DTOs;

public sealed record SettlementLineDto(
    Guid LineId,
    int LineNo,
    string CategoryCode,
    decimal GrossAmount,
    bool IsVat,
    decimal VatAmount,
    decimal NetAmount,
    string? Notes,
    string? CarPlate,
    decimal? OdometerKm);

public sealed record SettlementDto(
    Guid RequestId,
    int Version,
    DateOnly SettlementDate,
    string Purpose,
    string SpenderId,
    string SpenderName,
    string WorkerId,
    string ApproverEmail,
    string Status,
    decimal TotalAmount,
    string? ApprovalComment,
    string? JournalBatchNumber,
    bool IsEditable,
    IReadOnlyList<SettlementLineDto> Lines);

/// <summary>Lightweight projection for the "My Settlements" list view — avoids shipping full line data to a list screen.</summary>
public sealed record SettlementSummaryDto(
    Guid RequestId,
    DateOnly SettlementDate,
    string Purpose,
    string Status,
    decimal TotalAmount);
