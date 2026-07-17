using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

/// <summary>
/// Vertical Slice 9 — Reject Settlement.
/// POST /api/v1/settlements/{settlementId}/reject wraps RejectSettlementCommandHandler
/// (Milestone 0.3). Body: { "comment": "..." } — mandatory, max 1000 chars.
/// Same authorization model as Approve (Approver role + email match, or System).
/// Rejected settlement retains its status as a distinct state (D-014) until the spender
/// explicitly reopens it (VS10).
/// </summary>
[Collection("Api")]
public sealed class RejectSettlementEndpointTests
{
    private readonly HttpClient _spenderClient;
    private readonly HttpClient _approverClient;

    public RejectSettlementEndpointTests(ApiWebApplicationFactory factory)
    {
        _spenderClient = factory.CreateClient();
        _approverClient = factory.CreateClientWithIdentity(ApiWebApplicationFactory.ApproverUser);
    }

    private async Task<Guid> CreateDraftAsync()
    {
        var body = new { settlementDate = "2026-07-17", purpose = "Reject test" };
        HttpResponseMessage r = await _spenderClient.PostAsJsonAsync("/api/v1/settlements", body);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        SettlementDto? dto = await r.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!.RequestId;
    }

    private async Task<Guid> CreateSubmittedAsync()
    {
        Guid id = await CreateDraftAsync();

        await _spenderClient.PostAsJsonAsync($"/api/v1/settlements/{id}/lines", new
        {
            categoryCode = "GOVERNMENT_FEES",
            grossAmount = 350m,
            isVat = false,
            notes = "Reject-test line",
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        });

        HttpResponseMessage submit = await _spenderClient.PostAsync(
            $"/api/v1/settlements/{id}/submit", content: null);
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        return id;
    }

    [Fact]
    public async Task Post_SubmittedSettlement_AsApprover_Returns200WithRejectedStatus()
    {
        Guid id = await CreateSubmittedAsync();

        HttpResponseMessage response = await _approverClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/reject",
            new { comment = "Receipts are missing the vendor stamp." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal("Rejected", dto!.Status);
        Assert.Equal("Receipts are missing the vendor stamp.", dto.ApprovalComment);
        Assert.False(dto.IsEditable);   // Rejected is read-only until Reopen (D-014)
    }

    [Fact]
    public async Task Post_SubmittedSettlement_AsSpender_Returns403()
    {
        Guid id = await CreateSubmittedAsync();

        HttpResponseMessage response = await _spenderClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/reject",
            new { comment = "Spender should not be able to reject." });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyComment_Returns400()
    {
        // RejectSettlementCommandValidator: Comment NotEmpty — caught at FluentValidation
        // step (D-040), before the handler is invoked.
        Guid id = await CreateSubmittedAsync();

        HttpResponseMessage response = await _approverClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/reject",
            new { comment = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_MissingComment_Returns400()
    {
        // Null comment — RejectSettlementCommandValidator also rejects this (NotEmpty
        // treats null the same as empty string in FluentValidation).
        Guid id = await CreateSubmittedAsync();

        HttpResponseMessage response = await _approverClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/reject",
            new { comment = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_DraftSettlement_AsApprover_Returns400()
    {
        // Settlement.Reject() calls EnsureStatus(Submitted) — InvalidSettlementStateException
        // (Domain-namespace) → 400 via D-031.
        Guid id = await CreateDraftAsync();

        HttpResponseMessage response = await _approverClient.PostAsJsonAsync(
            $"/api/v1/settlements/{id}/reject",
            new { comment = "Wrong status." });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownSettlementId_AsApprover_Returns404()
    {
        HttpResponseMessage response = await _approverClient.PostAsJsonAsync(
            $"/api/v1/settlements/{Guid.NewGuid()}/reject",
            new { comment = "Does not matter." });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
