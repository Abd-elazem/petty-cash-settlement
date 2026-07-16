namespace PettyCash.Api.Endpoints;

/// <summary>
/// Wire-format request body for POST /api/v1/settlements/{settlementId}/lines. Mirrors
/// AddLineCommand's own public shape (minus SettlementId, which comes from the route) —
/// same D-016 rule as CreateSettlementRequest: no identity field, and no server-computed
/// field (VatAmount/NetAmount) accepted from the client either, since AddLineCommandHandler
/// computes those itself from IVatConfiguration.
///
/// No separate Api-layer response DTO, consistent with D-024/CreateSettlementRequest: the
/// response reuses PettyCash.Application.DTOs.SettlementDto (the updated parent Settlement)
/// directly.
/// </summary>
public sealed record AddSettlementLineRequest(
    string CategoryCode,
    decimal GrossAmount,
    bool IsVat,
    string? Notes,
    string? CarPlate,
    decimal? OdometerKm);
