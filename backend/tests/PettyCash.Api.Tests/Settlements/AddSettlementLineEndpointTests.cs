using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

/// <summary>
/// Vertical Slice 2 — Add Settlement Line. Reuses the existing "Api" collection/fixture
/// (ApiWebApplicationFactory, CollectionDefinition declared in CreateSettlementEndpointTests.cs)
/// — no new Testcontainers wiring needed. Uses the dev seed data's category mappings
/// (CategoryMappingConfiguration): OFFICE_SUPPLIES (non-fuel) and FUEL (KmRequired), exactly
/// as documented in CHANGELOG.md/docs/TODO.md for this slice. 6 tests, matching the corrected
/// count in CHANGELOG.md (an earlier 8-test draft included a "fuel missing odometer" and an
/// "empty category" case that were dropped here).
/// </summary>
[Collection("Api")]
public sealed class AddSettlementLineEndpointTests
{
    private readonly HttpClient _client;

    public AddSettlementLineEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateDraftSettlementAsync()
    {
        var request = new { settlementDate = "2026-07-15", purpose = "Site visit fuel and supplies" };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/settlements", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!.RequestId;
    }

    [Fact]
    public async Task Post_NonFuelLine_Returns200WithUpdatedSettlement()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
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

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal(settlementId, dto!.RequestId);
        Assert.Single(dto.Lines);
        Assert.Equal(150m, dto.TotalAmount);
    }

    [Fact]
    public async Task Post_FuelLineWithOdometer_Returns200WithUpdatedSettlement()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        var request = new
        {
            categoryCode = "FUEL",
            grossAmount = 300m,
            isVat = true,
            notes = "Delivery van fill-up",
            carPlate = "ABC-1234",
            odometerKm = 45210m,
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync($"/api/v1/settlements/{settlementId}/lines", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Single(dto!.Lines);
        Assert.Equal(300m, dto.TotalAmount);
    }

    [Fact]
    public async Task Post_ZeroAmount_Returns400()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        var request = new
        {
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount = 0m,
            isVat = false,
            notes = (string?)null,
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync($"/api/v1/settlements/{settlementId}/lines", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_CarPlateWithoutOdometer_Returns400()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        var request = new
        {
            categoryCode = "FUEL",
            grossAmount = 300m,
            isVat = false,
            notes = (string?)null,
            carPlate = "ABC-1234",
            odometerKm = (decimal?)null,
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync($"/api/v1/settlements/{settlementId}/lines", request);

        // AddLineCommandValidator's pairing rule (CarPlate/OdometerKm must both be present
        // or both absent) — a data-shape check, not the Domain's fuel-requires-odometer rule.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownCategory_Returns404()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        var request = new
        {
            categoryCode = "NOT_A_REAL_CATEGORY",
            grossAmount = 100m,
            isVat = false,
            notes = (string?)null,
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync($"/api/v1/settlements/{settlementId}/lines", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownSettlement_Returns404()
    {
        var unknownSettlementId = Guid.NewGuid();
        var request = new
        {
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount = 100m,
            isVat = false,
            notes = (string?)null,
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync($"/api/v1/settlements/{unknownSettlementId}/lines", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
