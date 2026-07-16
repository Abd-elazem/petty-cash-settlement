using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

[Collection("Api")]
public sealed class CreateSettlementEndpointTests
{
    private readonly HttpClient _client;

    public CreateSettlementEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_ValidRequest_Returns201WithDraftSettlement()
    {
        var request = new { settlementDate = "2026-07-15", purpose = "Site visit fuel and supplies" };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/settlements", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        Assert.Equal("Draft", dto!.Status);
        Assert.Equal("Site visit fuel and supplies", dto.Purpose);
        Assert.Equal(1, dto.Version);
        Assert.Empty(dto.Lines);
        // Matches AppUserProfileConfiguration's seeded row, which
        // DevelopmentCurrentUserContext's UserId was fixed to resolve against (D-038).
        Assert.Equal("spender.demo", dto.SpenderId);
        Assert.Equal("W-0001", dto.WorkerId);
        Assert.Equal("manager.demo@canex.com", dto.ApproverEmail);
        Assert.True(dto.IsEditable);
    }

    [Fact]
    public async Task Post_EmptyPurpose_Returns400WithValidationErrors()
    {
        var request = new { settlementDate = "2026-07-15", purpose = "" };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/settlements", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_DefaultSettlementDate_Returns400()
    {
        var request = new { settlementDate = "0001-01-01", purpose = "Valid purpose" };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/settlements", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_PurposeOverMaxLength_Returns400()
    {
        var request = new { settlementDate = "2026-07-15", purpose = new string('x', 501) };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/settlements", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<ApiWebApplicationFactory>
{
}
