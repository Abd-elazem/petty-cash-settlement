using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

/// <summary>
/// Vertical Slice 10 — Reopen Settlement.
/// POST /api/v1/settlements/{settlementId}/reopen wraps ReopenSettlementCommandHandler
/// (Milestone 0.3). Only the owning spender may reopen (EnsureCanReopen → EnsureOwner,
/// D-014). Moves Rejected → Draft, increments Version. The dev spender identity
/// ("spender.demo") owns every settlement created in these tests, so the standard
/// factory.CreateClient() is sufficient — no identity override needed for VS10's happy path.
///
/// The full Rejected→Reopen path requires first creating, adding a line, submitting,
/// and then rejecting (as approver). The reopen call itself is the spender's.
/// </summary>
[Collection("Api")]
public sealed class ReopenSettlementEndpointTests
{
    private readonly HttpClient _spenderClient;
    private readonly HttpClient _approverClient;

    public ReopenSettlementEndpointTests(ApiWebApplicationFactory factory)
    {
        _spenderClient = factory.CreateClient();
        _approverClient = factory.CreateClientWithIdentity(ApiWebApplicationFactory.ApproverUser);
    }

    private async Task<Guid> CreateDraftAsync()
    {
        var body = new { settlementDate = "2026-07-17", purpose = "Reopen test" };
        HttpResponseMessage r = await _spenderClient.PostAsJsonAsync("/api/v1/settlements", body);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        SettlementDto? dto = await r.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!.RequestId;
    }

    private async Task<Guid> CreateRejectedAsync()
    {
        Guid id = await CreateDraftAsync();

        await _spenderClient.PostAsJsonAsync($"/api/v1/settlements/{id}/lines", new
        {
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount = 100m,
            isVat = false,
            notes = "Reopen-test line",
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        });

        await _spenderClient.PostAsync($"/api/v1/settlements/{id}/submit", content: null);

        HttpResponseMessage reject = await _approverClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/reject",
            new { comment = "Please correct the amounts." });
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);
        return id;
    }

    [Fact]
    public async Task Post_RejectedSettlement_AsSpender_Returns200WithDraftStatus()
    {
        Guid id = await CreateRejectedAsync();

        HttpResponseMessage response = await _spenderClient.PostAsync(
            $"/api/v1/settlements/{id}/reopen", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal("Draft", dto!.Status);
        Assert.True(dto.IsEditable);
        // Version increments on reopen — D-014. The settlement was created at Version 1,
        // submitted, rejected (Version still 1), then reopened → Version 2.
        Assert.Equal(2, dto.Version);
    }

    [Fact]
    public async Task Post_RejectedSettlement_RetainsLinesAfterReopen()
    {
        // Lines are preserved when reopening — the spender edits the existing draft,
        // they don't start from scratch. Domain's ReopenForEdit() only changes status/version.
        Guid id = await CreateRejectedAsync();

        HttpResponseMessage response = await _spenderClient.PostAsync(
            $"/api/v1/settlements/{id}/reopen", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Single(dto!.Lines);     // the line added before submit is still there
        Assert.Equal(100m, dto.TotalAmount);
    }

    [Fact]
    public async Task Post_DraftSettlement_Returns400()
    {
        // ReopenForEdit() calls EnsureStatus(Rejected) — Draft fails it.
        // InvalidSettlementStateException (Domain-namespace) → 400 via D-031.
        Guid id = await CreateDraftAsync();

        HttpResponseMessage response = await _spenderClient.PostAsync(
            $"/api/v1/settlements/{id}/reopen", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownSettlementId_Returns404()
    {
        HttpResponseMessage response = await _spenderClient.PostAsync(
            $"/api/v1/settlements/{Guid.NewGuid()}/reopen", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_ReopenedSettlement_CanBeResubmitted()
    {
        // End-to-end: Draft → Submit → Reject → Reopen → (edit) → Submit again.
        // This is the full spender correction cycle described in ARCHITECTURE.md §10.
        Guid id = await CreateRejectedAsync();

        // Reopen
        HttpResponseMessage reopen = await _spenderClient.PostAsync(
            $"/api/v1/settlements/{id}/reopen", content: null);
        Assert.Equal(HttpStatusCode.OK, reopen.StatusCode);

        // Submit again (lines still present from before)
        HttpResponseMessage submit = await _spenderClient.PostAsync(
            $"/api/v1/settlements/{id}/submit", content: null);
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        SettlementDto? dto = await submit.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal("Submitted", dto!.Status);
        Assert.Equal(2, dto.Version);   // version incremented at reopen, not at submit
    }
}
