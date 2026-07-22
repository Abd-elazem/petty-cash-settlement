using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

/// <summary>
/// Vertical Slice 12 — Update Settlement Header.
/// PUT /api/v1/settlements/{settlementId} wraps UpdateSettlementHeaderCommandHandler.
/// The route carries the settlement ID; the body carries the new SettlementDate and Purpose.
///
/// Error paths:
///   - Unknown settlement ID → 404 (NotFoundException → GlobalExceptionHandler)
///   - Non-owner or non-Draft status → 403 (ForbiddenException → GlobalExceptionHandler)
///   - Invalid body (empty purpose, default date, over-length purpose) → 400 (FluentValidation)
/// </summary>
[Collection("Api")]
public sealed class UpdateSettlementHeaderEndpointTests
{
    private readonly HttpClient _client;

    public UpdateSettlementHeaderEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateDraftAsync(string purpose = "Original purpose")
    {
        var request = new { settlementDate = "2026-07-21", purpose };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/settlements", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!.RequestId;
    }

    // ── Happy path ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_ValidUpdate_Returns200WithUpdatedHeader()
    {
        Guid id = await CreateDraftAsync("Original purpose");

        var update = new { settlementDate = "2026-08-01", purpose = "Updated purpose" };
        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/settlements/{id}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal("Updated purpose", dto!.Purpose);
        Assert.Equal(new DateOnly(2026, 8, 1), dto.SettlementDate);
        Assert.Equal("Draft", dto.Status);
        // Lines and total unchanged — only header fields mutated.
        Assert.Empty(dto.Lines);
        Assert.Equal(0m, dto.TotalAmount);
    }

    [Fact]
    public async Task Put_UpdatePreservesExistingLines()
    {
        Guid id = await CreateDraftAsync("Purpose before update");

        // Add a line first so we can confirm it survives the header update.
        var lineRequest = new
        {
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount = 150m,
            isVat = false,
            notes = "Test line",
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        };
        HttpResponseMessage addResponse = await _client.PostAsJsonAsync($"/api/v1/settlements/{id}/lines", lineRequest);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var update = new { settlementDate = "2026-09-10", purpose = "Purpose after update" };
        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/settlements/{id}", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal("Purpose after update", dto!.Purpose);
        Assert.Equal(new DateOnly(2026, 9, 10), dto.SettlementDate);
        Assert.Single(dto.Lines);
        Assert.Equal(150m, dto.TotalAmount);
    }

    // ── Not found ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_UnknownSettlementId_Returns404()
    {
        var update = new { settlementDate = "2026-08-01", purpose = "Whatever" };
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/v1/settlements/{Guid.NewGuid()}", update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Validation ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Put_EmptyPurpose_Returns400()
    {
        Guid id = await CreateDraftAsync();

        var update = new { settlementDate = "2026-08-01", purpose = "" };
        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/settlements/{id}", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Put_DefaultDate_Returns400()
    {
        Guid id = await CreateDraftAsync();

        var update = new { settlementDate = "0001-01-01", purpose = "Valid purpose" };
        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/settlements/{id}", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_PurposeOverMaxLength_Returns400()
    {
        Guid id = await CreateDraftAsync();

        var update = new { settlementDate = "2026-08-01", purpose = new string('x', 501) };
        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/settlements/{id}", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
