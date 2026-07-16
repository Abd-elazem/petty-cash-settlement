using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

/// <summary>
/// Vertical Slice 3 — Submit Settlement. Reuses the existing "Api" collection/fixture
/// (ApiWebApplicationFactory), same as Slices 1/2 — no new Testcontainers wiring needed.
/// Draft → Submitted requires at least one line (Settlement.Submit()'s own Domain rule),
/// so most tests here first POST a settlement, then POST a line, before submitting —
/// exercising the three endpoints together the way a real client would.
/// </summary>
[Collection("Api")]
public sealed class SubmitSettlementEndpointTests
{
    private readonly HttpClient _client;

    public SubmitSettlementEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateDraftSettlementAsync()
    {
        var request = new { settlementDate = "2026-07-15", purpose = "Fuel for delivery van" };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/settlements", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!.RequestId;
    }

    private async Task AddNonFuelLineAsync(Guid settlementId)
    {
        var request = new
        {
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount = 150m,
            isVat = false,
            notes = "Printer paper",
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        };
        HttpResponseMessage response = await _client.PostAsJsonAsync($"/api/v1/settlements/{settlementId}/lines", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_DraftWithLine_ReturnsSubmittedSettlement()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        await AddNonFuelLineAsync(settlementId);

        HttpResponseMessage response = await _client.PostAsync($"/api/v1/settlements/{settlementId}/submit", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal("Submitted", dto!.Status);
        Assert.Equal(settlementId, dto.RequestId);
        Assert.False(dto.IsEditable);
        Assert.Single(dto.Lines);
    }

    [Fact]
    public async Task Post_DraftWithNoLines_Returns400()
    {
        Guid settlementId = await CreateDraftSettlementAsync();

        HttpResponseMessage response = await _client.PostAsync($"/api/v1/settlements/{settlementId}/submit", content: null);

        // Settlement.Submit()'s own Domain rule ("cannot submit a settlement with no
        // lines") throws DomainValidationException — mapped to 400 by
        // GlobalExceptionHandler's namespace-string match (D-031), same path AddLine's
        // fuel/odometer rule already exercises.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownSettlementId_Returns404()
    {
        var unknownId = Guid.NewGuid();

        HttpResponseMessage response = await _client.PostAsync($"/api/v1/settlements/{unknownId}/submit", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_AlreadySubmittedSettlement_Returns400()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        await AddNonFuelLineAsync(settlementId);

        HttpResponseMessage first = await _client.PostAsync($"/api/v1/settlements/{settlementId}/submit", content: null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Second submit attempt: Settlement.Submit()'s EnsureStatus(Draft) now fails
        // because status is already Submitted — InvalidSettlementStateException, also
        // Domain-namespace, also mapped to 400.
        HttpResponseMessage second = await _client.PostAsync($"/api/v1/settlements/{settlementId}/submit", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }
}
