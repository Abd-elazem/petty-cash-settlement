using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

/// <summary>
/// Vertical Slice 8 — Approve Settlement.
/// POST /api/v1/settlements/{settlementId}/approve wraps ApproveSettlementCommandHandler
/// (Milestone 0.3). Caller must be UserRole.Approver with an email matching the
/// settlement's ApproverEmailSnapshot, or UserRole.System (D-006 Power Automate callback).
///
/// Authorization identity split (see ApiWebApplicationFactory for rationale):
/// - Spender flow (create/add-line/submit): factory.CreateClient()
///   → DevelopmentCurrentUserContext → "spender.demo", Spender role.
/// - Approve flow: factory.CreateClientWithIdentity(ApiWebApplicationFactory.ApproverUser)
///   → FixedCurrentUserContext → "approver.test", Approver role,
///     email "manager.demo@canex.com" (matches the seeded AppUserProfile's ApproverEmail).
///
/// No production code changes — ApiWebApplicationFactory.CreateClientWithIdentity replaces
/// ICurrentUserContext in the test service collection only, for that client's requests.
/// </summary>
[Collection("Api")]
public sealed class ApproveSettlementEndpointTests
{
    private readonly HttpClient _spenderClient;
    private readonly HttpClient _approverClient;

    public ApproveSettlementEndpointTests(ApiWebApplicationFactory factory)
    {
        _spenderClient = factory.CreateClient();
        _approverClient = factory.CreateClientWithIdentity(ApiWebApplicationFactory.ApproverUser);
    }

    private async Task<Guid> CreateDraftAsync()
    {
        var body = new { settlementDate = "2026-07-17", purpose = "Approve test" };
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
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount = 200m,
            isVat = false,
            notes = "Approve-test line",
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        });

        HttpResponseMessage submit = await _spenderClient.PostAsync(
            $"/api/v1/settlements/{id}/submit", content: null);
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        return id;
    }

    [Fact]
    public async Task Post_SubmittedSettlement_AsApprover_Returns200WithApprovedStatus()
    {
        Guid id = await CreateSubmittedAsync();

        HttpResponseMessage response = await _approverClient.PostAsync(
            $"/api/v1/settlements/{id}/approve", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal("Approved", dto!.Status);
        Assert.Equal(id, dto.RequestId);
        Assert.False(dto.IsEditable);
    }

    [Fact]
    public async Task Post_SubmittedSettlement_AsSpender_Returns403()
    {
        // Spender does not have UserRole.Approver → EnsureCanApproveOrReject → ForbiddenException → 403.
        Guid id = await CreateSubmittedAsync();

        HttpResponseMessage response = await _spenderClient.PostAsync(
            $"/api/v1/settlements/{id}/approve", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_DraftSettlement_AsApprover_Returns400()
    {
        // Settlement.Approve() calls EnsureStatus(Submitted) — InvalidSettlementStateException
        // (Domain-namespace) → 400 via GlobalExceptionHandler D-031 namespace match.
        Guid id = await CreateDraftAsync();

        HttpResponseMessage response = await _approverClient.PostAsync(
            $"/api/v1/settlements/{id}/approve", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownSettlementId_AsApprover_Returns404()
    {
        HttpResponseMessage response = await _approverClient.PostAsync(
            $"/api/v1/settlements/{Guid.NewGuid()}/approve", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_AlreadyApprovedSettlement_Returns400()
    {
        // Double-approve is a Domain state-machine violation (status is now Approved,
        // not Submitted) → InvalidSettlementStateException → 400.
        Guid id = await CreateSubmittedAsync();
        await _approverClient.PostAsync($"/api/v1/settlements/{id}/approve", content: null);

        HttpResponseMessage response = await _approverClient.PostAsync(
            $"/api/v1/settlements/{id}/approve", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
