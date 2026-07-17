namespace PettyCash.Api.Endpoints;

/// <summary>
/// Request body for POST /api/v1/settlements/{settlementId}/reject.
/// A rejection comment is mandatory — RejectSettlementCommandValidator and
/// Settlement.Reject() both enforce this, so an empty or missing Comment will be
/// caught at the explicit-validation step before the handler is invoked (D-040).
/// </summary>
public sealed record RejectSettlementRequest(string Comment);
