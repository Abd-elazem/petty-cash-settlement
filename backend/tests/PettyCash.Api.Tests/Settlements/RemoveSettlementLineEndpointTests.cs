using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

/// <summary>
/// Vertical Slice 7 — Remove Settlement Line.
/// DELETE /api/v1/settlements/{settlementId}/lines/{lineId} wraps RemoveLineCommandHandler
/// (Milestone 0.3). Both IDs are route-bound; no request body. Returns 200 with the
/// updated SettlementDto (line removed, TotalAmount recomputed) — same reasoning as
/// AddLine/UpdateLine: the line is not independently addressable (D-003/D-042).
/// </summary>
[Collection("Api")]
public sealed class RemoveSettlementLineEndpointTests
{
    private readonly HttpClient _client;

    public RemoveSettlementLineEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateDraftSettlementAsync()
    {
        var request = new { settlementDate = "2026-07-16", purpose = "Remove line test" };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/settlements", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!.RequestId;
    }

    private async Task<Guid> AddLineAsync(Guid settlementId, decimal amount = 100m)
    {
        var request = new
        {
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount = amount,
            isVat = false,
            notes = (string?)null,
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
    public async Task Delete_ExistingLine_Returns200WithLineRemoved()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        Guid lineId = await AddLineAsync(settlementId, 150m);

        HttpResponseMessage response = await _client.DeleteAsync(
            $"/api/v1/settlements/{settlementId}/lines/{lineId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Empty(dto!.Lines);
        Assert.Equal(0m, dto.TotalAmount);
    }

    [Fact]
    public async Task Delete_OneOfTwoLines_Returns200WithRemainingLine()
    {
        Guid settlementId = await CreateDraftSettlementAsync();
        Guid lineId1 = await AddLineAsync(settlementId, 100m);
        Guid lineId2 = await AddLineAsync(settlementId, 200m);

        HttpResponseMessage response = await _client.DeleteAsync(
            $"/api/v1/settlements/{settlementId}/lines/{lineId1}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Single(dto!.Lines);
        Assert.Equal(lineId2, dto.Lines[0].LineId);
        Assert.Equal(200m, dto.TotalAmount);
    }

    [Fact]
    public async Task Delete_UnknownSettlementId_Returns404()
    {
        var unknownSettlementId = Guid.NewGuid();
        var unknownLineId = Guid.NewGuid();

        HttpResponseMessage response = await _client.DeleteAsync(
            $"/api/v1/settlements/{unknownSettlementId}/lines/{unknownLineId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownLineId_Returns400()
    {
        // RemoveLine on a settlement that exists but with an unknown LineId throws
        // a Domain exception (Settlement.RemoveLine validates the LineId exists), which
        // GlobalExceptionHandler maps to 400 via the namespace-string match (D-031).
        Guid settlementId = await CreateDraftSettlementAsync();
        var unknownLineId = Guid.NewGuid();

        HttpResponseMessage response = await _client.DeleteAsync(
            $"/api/v1/settlements/{settlementId}/lines/{unknownLineId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
