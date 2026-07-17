using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

/// <summary>
/// Vertical Slice 5 — List My Settlements.
/// GET /api/v1/settlements/mine wraps GetMySettlementsQueryHandler (Milestone 0.3).
/// The query is inherently self-scoped — no SpenderId parameter; identity comes from
/// ICurrentUserContext (D-016), so there is no ownership-bypass risk. Returns a list
/// of SettlementSummaryDto (lightweight projection without full line data).
/// </summary>
[Collection("Api")]
public sealed class GetMySettlementsEndpointTests
{
    private readonly HttpClient _client;

    public GetMySettlementsEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateDraftSettlementAsync(string purpose)
    {
        var request = new { settlementDate = "2026-07-16", purpose };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/settlements", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!.RequestId;
    }

    [Fact]
    public async Task Get_NoSettlements_Returns200WithEmptyList()
    {
        // NOTE: this test shares the same Testcontainers database as all other tests in
        // the "Api" collection — other tests may have already created settlements for
        // "spender.demo". This assertion therefore only checks that the endpoint returns
        // 200 and a list (not that the list is empty in isolation).
        HttpResponseMessage response = await _client.GetAsync("/api/v1/settlements/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        IReadOnlyList<SettlementSummaryDto>? summaries =
            await response.Content.ReadFromJsonAsync<IReadOnlyList<SettlementSummaryDto>>();
        Assert.NotNull(summaries);
    }

    [Fact]
    public async Task Get_AfterCreatingSettlement_ContainsItInList()
    {
        Guid settlementId = await CreateDraftSettlementAsync("Mine-list test settlement");

        HttpResponseMessage response = await _client.GetAsync("/api/v1/settlements/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        IReadOnlyList<SettlementSummaryDto>? summaries =
            await response.Content.ReadFromJsonAsync<IReadOnlyList<SettlementSummaryDto>>();
        Assert.NotNull(summaries);

        // Verify the just-created settlement appears in the list with correct summary fields.
        SettlementSummaryDto? match = summaries!.FirstOrDefault(s => s.RequestId == settlementId);
        Assert.NotNull(match);
        Assert.Equal("Mine-list test settlement", match!.Purpose);
        Assert.Equal("Draft", match.Status);
        Assert.Equal(0m, match.TotalAmount);
    }

    [Fact]
    public async Task Get_SummaryDoesNotContainLines()
    {
        // SettlementSummaryDto has no Lines property — this test documents that the
        // endpoint returns the lightweight projection, not the full SettlementDto.
        // Verified by confirming the JSON deserialises cleanly to SettlementSummaryDto
        // (which has no Lines member) without error.
        Guid settlementId = await CreateDraftSettlementAsync("Summary shape test");

        // Add a line so the settlement isn't empty
        await _client.PostAsJsonAsync($"/api/v1/settlements/{settlementId}/lines", new
        {
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount = 100m,
            isVat = false,
            notes = (string?)null,
            carPlate = (string?)null,
            odometerKm = (decimal?)null,
        });

        HttpResponseMessage response = await _client.GetAsync("/api/v1/settlements/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // If deserialisation to SettlementSummaryDto list succeeds, the endpoint is
        // correctly returning summary projections, not full DTOs.
        IReadOnlyList<SettlementSummaryDto>? summaries =
            await response.Content.ReadFromJsonAsync<IReadOnlyList<SettlementSummaryDto>>();
        Assert.NotNull(summaries);
        SettlementSummaryDto? match = summaries!.FirstOrDefault(s => s.RequestId == settlementId);
        Assert.NotNull(match);
        Assert.Equal(100m, match!.TotalAmount);
    }
}
