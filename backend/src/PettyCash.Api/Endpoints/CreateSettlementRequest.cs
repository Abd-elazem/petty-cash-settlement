namespace PettyCash.Api.Endpoints;

/// <summary>
/// Wire-format request body for POST /api/v1/settlements. Deliberately excludes any
/// spender/identity field, mirroring CreateDraftSettlementCommand's own rule (D-016) —
/// the caller cannot specify who they are creating the settlement as; identity is always
/// server-resolved via ICurrentUserContext.
///
/// No separate Api-layer response DTO exists alongside this: the response reuses
/// PettyCash.Application.DTOs.SettlementDto directly. Application already returns a
/// clean, infrastructure-free DTO built for exactly this purpose, and D-024 already
/// established the project's preference against parallel DTO classes that add no
/// behavior of their own.
/// </summary>
public sealed record CreateSettlementRequest(DateOnly SettlementDate, string Purpose);
