namespace PettyCash.Api.Endpoints;

/// <summary>
/// Wire-format request body for PUT /api/v1/settlements/{settlementId}.
///
/// Carries only the two header fields the spender originally authored: SettlementDate
/// and Purpose. Identity snapshots (SpenderName, WorkerId, ApproverEmail) are fixed at
/// creation time per Guide §5.2 and are not accepted here.
/// </summary>
public sealed record UpdateSettlementHeaderRequest(DateOnly SettlementDate, string Purpose);
