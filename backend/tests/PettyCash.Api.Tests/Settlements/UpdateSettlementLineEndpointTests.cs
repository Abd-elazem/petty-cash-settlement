using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

/// <summary>
/// Vertical Slice 6 — Update Settlement Line.
/// PUT /api/v1/settlements/{settlementId}/lines/{lineId} wraps UpdateLineCommandHandler
/// (Milestone 0.3). Both IDs are route-bound; the body carries the new field values.
/// Error paths mirror AddLine: unknown settlement → 404, unknown category → 404,
/// invalid body → 400 (FluentValidation), Domain rule violation → 400 (namespace match).
/// </summary>
[Collection("Api")]
public sealed class UpdateSettlementLineEndpointTests
{
    private readonly HttpClient _client;

    public UpdateSettlementLineEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateDraftSettlementAsync()
    {
        var request = new { settlementDate = "2026-07-16", purpose = "Update line test" };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/settlements", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!.RequestId;
    }

    private async Task<Guid> AddLineAsync(Guid settlementId, string categoryCode = "OFFICE_SUPPLIES", decimal amount = 100m)
    {
        var request = new
        {
            categoryCode,
            grossAmount = amount,
            isVat = false,
            notes = "original note",
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        };
        HttpResponseMessage response = await _client.PostAsJsonAsync($"/api/v1/settlements/{settlementId}/lines", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!.Lines[^1].LineId;
    }

    [Fact]
    public async Task Put_ValidNonFuelUpdate_Returns200WithUpdatedLine()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        Guid lineId = await AddLineAsync(settlementId, "OFFICE_SUPPLIES", 100m);

        var updateRequest = new
        {
            categoryCode = "GOVERNMENT_FEES",
            grossAmount = 250m,
            isVat = false,
            notes = "Updated note",
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/v1/settlements/{settlementId}/lines/{lineId}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Single(dto!.Lines);
        Assert.Equal("GOVERNMENT_FEES", dto.Lines[0].CategoryCode);
        Assert.Equal(250m, dto.Lines[0].GrossAmount);
        Assert.Equal("Updated note", dto.Lines[0].Notes);
        Assert.Equal(250m, dto.TotalAmount);
    }

    [Fact]
    public async Task Put_ChangeCategoryToFuelWithOdometer_Returns200()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        Guid lineId = await AddLineAsync(settlementId, "OFFICE_SUPPLIES", 100m);

        var updateRequest = new
        {
            categoryCode = "FUEL",
            grossAmount = 300m,
            isVat = true,
            notes = "Van fill-up",
            carPlate = "XYZ-9999",
            odometerKm = 51000m,
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/v1/settlements/{settlementId}/lines/{lineId}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal("FUEL", dto!.Lines[0].CategoryCode);
        Assert.Equal("XYZ-9999", dto.Lines[0].CarPlate);
        Assert.Equal(51000m, dto.Lines[0].OdometerKm);
    }

    [Fact]
    public async Task Put_ZeroAmount_Returns400()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        Guid lineId = await AddLineAsync(settlementId);

        var updateRequest = new
        {
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount = 0m,
            isVat = false,
            notes = (string?)null,
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/v1/settlements/{settlementId}/lines/{lineId}", updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_CarPlateWithoutOdometer_Returns400()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        Guid lineId = await AddLineAsync(settlementId);

        var updateRequest = new
        {
            categoryCode = "FUEL",
            grossAmount = 200m,
            isVat = false,
            notes = (string?)null,
            carPlate = "ABC-1234",
            odometerKm = (decimal?)null,  // missing — FluentValidation paired-fields rule
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/v1/settlements/{settlementId}/lines/{lineId}", updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UnknownSettlementId_Returns404()
    {
        var unknownSettlementId = Guid.NewGuid();
        var unknownLineId = Guid.NewGuid();

        var updateRequest = new
        {
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount = 100m,
            isVat = false,
            notes = (string?)null,
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/v1/settlements/{unknownSettlementId}/lines/{unknownLineId}", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UnknownCategory_Returns404()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        Guid lineId = await AddLineAsync(settlementId);

        var updateRequest = new
        {
            categoryCode = "NOT_A_REAL_CATEGORY",
            grossAmount = 100m,
            isVat = false,
            notes = (string?)null,
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/v1/settlements/{settlementId}/lines/{lineId}", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
