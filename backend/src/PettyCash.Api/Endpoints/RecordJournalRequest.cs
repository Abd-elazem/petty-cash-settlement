namespace PettyCash.Api.Endpoints;

/// <summary>
/// Request body for POST /api/v1/settlements/{settlementId}/journal.
/// Called by the Power Automate flow after it creates the D365FO journal (Guide §6.2,
/// D-006). The JournalBatchNumber written back here is the D365FO journal batch number
/// — RecordJournalCommandValidator enforces it is non-empty; the handler (D-018)
/// implements idempotency so a retried flow call with the same number is a safe no-op.
/// </summary>
public sealed record RecordJournalRequest(string JournalBatchNumber);
