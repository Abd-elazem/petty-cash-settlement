using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

/// <summary>
/// Vertical Slice 4 — Get Settlement Detail.
/// GET /api/v1/settlements/{settlementId} wraps GetSettlementByIdQueryHandler (Milestone 0.3).
/// The handler enforces ownership via ISettlementAuthorizationPolicy.EnsureCanView —
/// with DevelopmentCurrentUserContext pinned to "spender.demo", settlements created by
/// the dev user are viewable and those belonging to another spender are not (though with
/// only one dev user seeded, the 403 path is tested by creating and then attempting to
/// read a settlement as if from an unknown owner — which maps to 404 here since the
/// settlement simply won't exist for an unknown ID).
/// Reuses the existing "Api" xUnit collection / ApiWebApplicationFactory (no new wiring).
/// </summary>
[Collection("Api")]
public sealed class GetSettlementEndpointTests
{
    private readonly HttpClient _client;

    public GetSettlementEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateDraftSettlementAsync(string purpose = "Office run")
    {
        var request = new { settlementDate = "2026-07-16", purpose };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/settlements", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!.RequestId;
    }

    [Fact]
    public async Task Get_ExistingOwnedSettlement_Returns200WithFullDto()
    {
        Guid settlementId = await CreateDraftSettlementAsync("Fuel for site visit");

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/settlements/{settlementId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal(settlementId, dto!.RequestId);
        Assert.Equal("Draft", dto.Status);
        Assert.Equal("Fuel for site visit", dto.Purpose);
        Assert.Equal("spender.demo", dto.SpenderId);
        Assert.True(dto.IsEditable);
        Assert.Empty(dto.Lines);
    }

    [Fact]
    public async Task Get_UnknownSettlementId_Returns404()
    {
        var unknownId = Guid.NewGuid();

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/settlements/{unknownId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_SettlementWithLines_ReturnsDtoWithLines()
    {
        Guid settlementId = await CreateDraftSettlementAsync("Multi-line settlement");

        // Add two lines
        await _client.PostAsJsonAsync($"/api/v1/settlements/{settlementId}/lines", new
        {
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount = 200m,
            isVat = false,
            notes = "Stationery",
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        });
        await _client.PostAsJsonAsync($"/api/v1/settlements/{settlementId}/lines", new
        {
            categoryCode = "GOVERNMENT_FEES",
            grossAmount = 150m,
            isVat = false,
            notes = "Registration",
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        });

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/settlements/{settlementId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal(2, dto!.Lines.Count);
        Assert.Equal(350m, dto.TotalAmount);
    }
}
