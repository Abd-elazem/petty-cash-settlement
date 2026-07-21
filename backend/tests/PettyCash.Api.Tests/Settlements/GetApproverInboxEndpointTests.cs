using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.Abstractions;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

[Collection("Api")]
public sealed class GetApproverInboxEndpointTests
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _spenderClient;
    private readonly HttpClient _approverClient;

    public GetApproverInboxEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _spenderClient = factory.CreateClient();
        _approverClient = factory.CreateClientWithIdentity(ApiWebApplicationFactory.ApproverUser);
    }

    [Fact]
    public async Task Get_AsApprover_ReturnsOnlySubmittedSettlementsAwaitingTheirDecision()
    {
        Guid submittedId = await CreateSubmittedAsync("Inbox submitted");
        Guid draftId = await CreateDraftAsync("Inbox draft");
        Guid approvedId = await CreateSubmittedAsync("Inbox approved");
        HttpResponseMessage approve = await _approverClient.PostAsync($"/api/v1/settlements/{approvedId}/approve", content: null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        HttpResponseMessage response = await _approverClient.GetAsync("/api/v1/settlements/inbox");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        IReadOnlyList<SettlementDto>? inbox = await response.Content.ReadFromJsonAsync<IReadOnlyList<SettlementDto>>();
        Assert.NotNull(inbox);

        Assert.Contains(inbox!, item => item.RequestId == submittedId);
        Assert.DoesNotContain(inbox!, item => item.RequestId == draftId);
        Assert.DoesNotContain(inbox!, item => item.RequestId == approvedId);
        Assert.All(inbox!, item => Assert.Equal("Submitted", item.Status));
    }

    [Fact]
    public async Task Get_AsSpender_Returns403()
    {
        HttpResponseMessage response = await _spenderClient.GetAsync("/api/v1/settlements/inbox");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_AsUnassignedApprover_ReturnsEmptyList()
    {
        await CreateSubmittedAsync("Assigned to seeded approver only");
        HttpClient otherApprover = _factory.CreateClientWithIdentity(new CurrentUser(
            UserId: "approver.other",
            Email: "other.manager@canex.com",
            Roles: new[] { UserRole.Approver }));

        HttpResponseMessage response = await otherApprover.GetAsync("/api/v1/settlements/inbox");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        IReadOnlyList<SettlementDto>? inbox = await response.Content.ReadFromJsonAsync<IReadOnlyList<SettlementDto>>();
        Assert.NotNull(inbox);
        Assert.Empty(inbox!);
    }

    [Fact]
    public async Task Get_MultiplePendingSubmittedSettlements_ReturnsAllMatchingRows()
    {
        Guid first = await CreateSubmittedAsync("Inbox pending 1");
        Guid second = await CreateSubmittedAsync("Inbox pending 2");

        HttpResponseMessage response = await _approverClient.GetAsync("/api/v1/settlements/inbox");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        IReadOnlyList<SettlementDto>? inbox = await response.Content.ReadFromJsonAsync<IReadOnlyList<SettlementDto>>();
        Assert.NotNull(inbox);
        Assert.Contains(inbox!, item => item.RequestId == first);
        Assert.Contains(inbox!, item => item.RequestId == second);
    }

    [Fact]
    public async Task Get_Anonymous_Returns401()
    {
        HttpClient anonymous = _factory.CreateAnonymousClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/v1/settlements/inbox");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Guid> CreateDraftAsync(string purpose)
    {
        HttpResponseMessage response = await _spenderClient.PostAsJsonAsync("/api/v1/settlements", new
        {
            settlementDate = "2026-07-19",
            purpose
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!.RequestId;
    }

    private async Task<Guid> CreateSubmittedAsync(string purpose)
    {
        Guid requestId = await CreateDraftAsync(purpose);

        HttpResponseMessage addLine = await _spenderClient.PostAsJsonAsync($"/api/v1/settlements/{requestId}/lines", new
        {
            categoryCode = "OFFICE_SUPPLIES",
            grossAmount = 150m,
            isVat = false,
            notes = purpose,
            carPlate = (string?)null,
            odometerKm = (decimal?)null
        });
        Assert.Equal(HttpStatusCode.OK, addLine.StatusCode);

        HttpResponseMessage submit = await _spenderClient.PostAsync($"/api/v1/settlements/{requestId}/submit", content: null);
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        return requestId;
    }
}
