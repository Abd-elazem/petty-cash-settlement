namespace PettyCash.Api.Endpoints;

/// <summary>
/// Request body for PUT /api/v1/settlements/{settlementId}/lines/{lineId}.
/// SettlementId and LineId are bound from the route, not this record — there is no way
/// for a client to post a body whose IDs disagree with the URL (same rationale as
/// AddSettlementLineRequest, D-042).
/// </summary>
public sealed record UpdateSettlementLineRequest(
    string CategoryCode,
    decimal GrossAmount,
    bool IsVat,
    string? Notes,
    string? CarPlate,
    decimal? OdometerKm);
