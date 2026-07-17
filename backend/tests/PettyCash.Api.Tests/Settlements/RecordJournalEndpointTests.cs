using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

/// <summary>
/// Vertical Slice 11 — Record Journal Entry.
/// POST /api/v1/settlements/{settlementId}/journal wraps RecordJournalCommandHandler
/// (Milestone 0.3, D-006/D-008/D-018).
///
/// Authorization: UserRole.System only (EnsureCanRecordJournal). This endpoint is the
/// Power Automate callback — it is not callable by a spender or approver.
/// Identity split:
///   - Spender flow (create/add-line/submit/reopen): factory.CreateClient()
///   - Approver flow (approve/reject):               factory.CreateClientWithIdentity(ApproverUser)
///   - System flow (record-journal):                 factory.CreateClientWithIdentity(SystemUser)
///
/// Key behaviors verified:
///   D-018 idempotency: same journal number on an already-Journalled settlement → 200 (safe retry).
///   D-018 conflict:    different journal number on an already-Journalled settlement → 400.
///   D-031 state error: settlement not in Approved status → 400 via Domain namespace match.
/// </summary>
[Collection("Api")]
public sealed class RecordJournalEndpointTests
{
    private readonly HttpClient _spenderClient;
    private readonly HttpClient _approverClient;
    private readonly HttpClient _systemClient;

    public RecordJournalEndpointTests(ApiWebApplicationFactory factory)
    {
        _spenderClient  = factory.CreateClient();
        _approverClient = factory.CreateClientWithIdentity(ApiWebApplicationFactory.ApproverUser);
        _systemClient   = factory.CreateClientWithIdentity(ApiWebApplicationFactory.SystemUser);
    }

    // ── Shared setup helpers ──────────────────────────────────────────────────────────

    private async Task<Guid> CreateDraftAsync()
    {
        var body = new { settlementDate = "2026-07-17", purpose = "Journal test" };
        HttpResponseMessage r = await _spenderClient.PostAsJsonAsync("/api/v1/settlements", body);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        SettlementDto? dto = await r.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!.RequestId;
    }

    /// <summary>Creates a settlement in the Approved state ready for journal write-back.</summary>
    private async Task<Guid> CreateApprovedAsync()
    {
        Guid id = await CreateDraftAsync();

        await _spenderClient.PostAsJsonAsync($"/api/v1/settlements/{id}/lines", new
        {
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount  = 500m,
            isVat        = false,
            notes        = "Journal-test line",
            carPlate     = (string?)null,
            odometerKm   = (decimal?)null,
        });

        await _spenderClient.PostAsync($"/api/v1/settlements/{id}/submit", content: null);

        HttpResponseMessage approve = await _approverClient.PostAsync(
            $"/api/v1/settlements/{id}/approve", content: null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        return id;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_ApprovedSettlement_AsSystem_Returns200WithJournalledStatus()
    {
        Guid id = await CreateApprovedAsync();

        HttpResponseMessage response = await _systemClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/journal",
            new { journalBatchNumber = "JNL-20260717-001" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal("Journalled", dto!.Status);
        Assert.Equal("JNL-20260717-001", dto.JournalBatchNumber);
        Assert.Equal(id, dto.RequestId);
        Assert.False(dto.IsEditable);
    }

    [Fact]
    public async Task Post_SameJournalNumber_OnAlreadyJournalled_Returns200Idempotent()
    {
        // D-018: a retried Power Automate call with the identical journal number must
        // be a safe no-op — same 200 response, no state change, no exception.
        Guid id = await CreateApprovedAsync();
        const string batchNumber = "JNL-IDEMPOTENT-001";

        HttpResponseMessage first = await _systemClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/journal", new { journalBatchNumber = batchNumber });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        HttpResponseMessage second = await _systemClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/journal", new { journalBatchNumber = batchNumber });

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        SettlementDto? dto = await second.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal("Journalled", dto!.Status);
        Assert.Equal(batchNumber, dto.JournalBatchNumber);
    }

    [Fact]
    public async Task Post_DifferentJournalNumber_OnAlreadyJournalled_Returns400()
    {
        // D-018: a DIFFERENT journal number on an already-Journalled settlement is a real
        // conflict (possible duplicate-journal upstream bug), not a safe retry.
        // Domain throws → GlobalExceptionHandler namespace match → 400.
        Guid id = await CreateApprovedAsync();

        await _systemClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/journal", new { journalBatchNumber = "JNL-FIRST" });

        HttpResponseMessage response = await _systemClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/journal", new { journalBatchNumber = "JNL-CONFLICT" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_SubmittedSettlement_NotYetApproved_Returns400()
    {
        // RecordJournal requires Approved status — Settlement.RecordJournal() enforces
        // this via EnsureStatus(Approved), throwing a Domain exception → 400.
        Guid id = await CreateDraftAsync();

        await _spenderClient.PostAsJsonAsync($"/api/v1/settlements/{id}/lines", new
        {
            categoryCode = "GOVERNMENT_FEES",
            grossAmount  = 100m,
            isVat        = false,
            notes        = (string?)null,
            carPlate     = (string?)null,
            odometerKm   = (decimal?)null,
        });
        await _spenderClient.PostAsync($"/api/v1/settlements/{id}/submit", content: null);

        // Settlement is Submitted but NOT Approved — journal write-back is premature.
        HttpResponseMessage response = await _systemClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/journal", new { journalBatchNumber = "JNL-PREMATURE" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_AsSpender_Returns403()
    {
        // EnsureCanRecordJournal: only UserRole.System is permitted.
        // Spender calling the journal endpoint must receive 403, not 400 or 404.
        Guid id = await CreateApprovedAsync();

        HttpResponseMessage response = await _spenderClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/journal", new { journalBatchNumber = "JNL-FORBIDDEN" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyJournalBatchNumber_Returns400()
    {
        // RecordJournalCommandValidator: JournalBatchNumber NotEmpty — caught at the
        // explicit-validation step (D-040) before the handler is invoked.
        Guid id = await CreateApprovedAsync();

        HttpResponseMessage response = await _systemClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/journal", new { journalBatchNumber = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownSettlementId_Returns404()
    {
        HttpResponseMessage response = await _systemClient.PostAsJsonAsync(
            $"/api/v1/settlements/{Guid.NewGuid()}/journal",
            new { journalBatchNumber = "JNL-NOTFOUND" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
