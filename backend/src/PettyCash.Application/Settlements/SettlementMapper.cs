using PettyCash.Application.DTOs;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Settlements;

/// <summary>Pure mapping, no I/O, no business rules — kept separate from handlers so every handler doesn't reimplement it.</summary>
public static class SettlementMapper
{
    public static SettlementDto ToDto(Settlement settlement)
    {
        return new SettlementDto(
            settlement.Id,
            settlement.Version,
            settlement.SettlementDate,
            settlement.Purpose,
            settlement.SpenderId,
            settlement.SpenderNameSnapshot,
            settlement.WorkerIdSnapshot,
            settlement.ApproverEmailSnapshot,
            settlement.Status.ToString(),
            settlement.TotalAmount,
            settlement.ApprovalComment,
            settlement.JournalBatchNumber,
            settlement.IsEditable,
            settlement.Lines.Select(ToDto).ToList());
    }

    public static SettlementLineDto ToDto(SettlementLine line)
    {
        return new SettlementLineDto(
            line.LineId,
            line.LineNo,
            line.CategoryCode,
            line.GrossAmount.Amount,
            line.IsVat,
            line.VatBreakdown.Vat,
            line.VatBreakdown.Net,
            line.Notes,
            line.Odometer?.CarPlate,
            line.Odometer?.OdometerKm);
    }

    public static SettlementSummaryDto ToSummaryDto(Settlement settlement)
    {
        return new SettlementSummaryDto(
            settlement.Id,
            settlement.SettlementDate,
            settlement.Purpose,
            settlement.Status.ToString(),
            settlement.TotalAmount);
    }
}
