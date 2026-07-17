using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.Abstractions;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

[Collection("Api")]
public sealed class AuthorizationCoverageEndpointTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AuthorizationCoverageEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public static IEnumerable<object[]> ProtectedEndpoints()
    {
        yield return new object[] { new EndpointCase("CreateDraft", HttpMethod.Post, "/api/v1/settlements", new { settlementDate = "2026-07-17", purpose = "Auth coverage" }) };
        yield return new object[] { new EndpointCase("AddLine", HttpMethod.Post, "/api/v1/settlements/{settlementId}/lines", new { categoryCode = "OFFICE_SUPPLIES", grossAmount = 100m, isVat = false, notes = "Auth", carPlate = (string?)null, odometerKm = (decimal?)null }) };
        yield return new object[] { new EndpointCase("Submit", HttpMethod.Post, "/api/v1/settlements/{settlementId}/submit", null) };
        yield return new object[] { new EndpointCase("GetMine", HttpMethod.Get, "/api/v1/settlements/mine", null) };
        yield return new object[] { new EndpointCase("GetById", HttpMethod.Get, "/api/v1/settlements/{settlementId}", null) };
        yield return new object[] { new EndpointCase("UpdateLine", HttpMethod.Put, "/api/v1/settlements/{settlementId}/lines/{lineId}", new { categoryCode = "OFFICE_SUPPLIES", grossAmount = 120m, isVat = false, notes = "Updated", carPlate = (string?)null, odometerKm = (decimal?)null }) };
        yield return new object[] { new EndpointCase("RemoveLine", HttpMethod.Delete, "/api/v1/settlements/{settlementId}/lines/{lineId}", null) };
        yield return new object[] { new EndpointCase("Approve", HttpMethod.Post, "/api/v1/settlements/{settlementId}/approve", null) };
        yield return new object[] { new EndpointCase("Reject", HttpMethod.Post, "/api/v1/settlements/{settlementId}/reject", new { comment = "Rejected for auth coverage" }) };
        yield return new object[] { new EndpointCase("Reopen", HttpMethod.Post, "/api/v1/settlements/{settlementId}/reopen", null) };
        yield return new object[] { new EndpointCase("RecordJournal", HttpMethod.Post, "/api/v1/settlements/{settlementId}/journal", new { journalBatchNumber = "JNL-AUTH-001" }) };
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoints_AnonymousRequests_Return401(EndpointCase endpoint)
    {
        HttpClient client = _factory.CreateAnonymousClient();
        HttpRequestMessage request = CreateRequest(endpoint, Guid.NewGuid(), Guid.NewGuid());

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoints_AuthenticatedWithoutRequiredRole_Return403(EndpointCase endpoint)
    {
        HttpClient client = _factory.CreateClientWithIdentity(new CurrentUser(
            UserId: "content.owner.test",
            Email: "content.owner@canex.local",
            Roles: new[] { UserRole.FinanceContentOwner }));

        HttpRequestMessage request = CreateRequest(endpoint, Guid.NewGuid(), Guid.NewGuid());

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoints_RequiredRoles_AllHaveSuccessfulPath()
    {
        HttpClient spenderClient = _factory.CreateClient();
        HttpClient approverClient = _factory.CreateClientWithIdentity(ApiWebApplicationFactory.ApproverUser);
        HttpClient systemClient = _factory.CreateClientWithIdentity(ApiWebApplicationFactory.SystemUser);
        HttpClient apAccountantClient = _factory.CreateClientWithIdentity(new CurrentUser(
            UserId: "ap.accountant.test",
            Email: "ap.accountant@canex.local",
            Roles: new[] { UserRole.ApAccountant }));

        // Create / Add / Update / Remove (Spender policy)
        SettlementDto editable = await CreateDraftAsync(spenderClient, "Auth success draft flow");
        SettlementDto withLine = await AddLineAsync(spenderClient, editable.RequestId, "Auth success line");
        SettlementLineDto line = Assert.Single(withLine.Lines);

        HttpResponseMessage update = await spenderClient.PutAsJsonAsync(
            $"/api/v1/settlements/{editable.RequestId}/lines/{line.LineId}",
            new { categoryCode = "OFFICE_SUPPLIES", grossAmount = 145m, isVat = false, notes = "Updated auth", carPlate = (string?)null, odometerKm = (decimal?)null });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        HttpResponseMessage remove = await spenderClient.DeleteAsync(
            $"/api/v1/settlements/{editable.RequestId}/lines/{line.LineId}");
        Assert.Equal(HttpStatusCode.OK, remove.StatusCode);

        // GetMine (Spender policy)
        HttpResponseMessage mine = await spenderClient.GetAsync("/api/v1/settlements/mine");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);

        // GetById (View policy: AP Accountant succeeds)
        SettlementDto viewSeed = await CreateDraftAsync(spenderClient, "Auth view flow");
        HttpResponseMessage getById = await apAccountantClient.GetAsync($"/api/v1/settlements/{viewSeed.RequestId}");
        Assert.Equal(HttpStatusCode.OK, getById.StatusCode);

        // Submit / Approve / Journal (Spender + Approver/System composite + System)
        SettlementDto journalSeed = await CreateDraftAsync(spenderClient, "Auth journal flow");
        await AddLineAsync(spenderClient, journalSeed.RequestId, "Journal line");

        HttpResponseMessage submit = await spenderClient.PostAsync(
            $"/api/v1/settlements/{journalSeed.RequestId}/submit", content: null);
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        HttpResponseMessage approve = await approverClient.PostAsync(
            $"/api/v1/settlements/{journalSeed.RequestId}/approve", content: null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        HttpResponseMessage journal = await systemClient.PostAsJsonAsync(
            $"/api/v1/settlements/{journalSeed.RequestId}/journal",
            new { journalBatchNumber = "JNL-AUTH-SUCCESS-001" });
        Assert.Equal(HttpStatusCode.OK, journal.StatusCode);

        // Reject / Reopen (Approver/System composite + Spender)
        SettlementDto reopenSeed = await CreateDraftAsync(spenderClient, "Auth reopen flow");
        await AddLineAsync(spenderClient, reopenSeed.RequestId, "Reopen line");
        await spenderClient.PostAsync($"/api/v1/settlements/{reopenSeed.RequestId}/submit", content: null);

        HttpResponseMessage reject = await approverClient.PostAsJsonAsync(
            $"/api/v1/settlements/{reopenSeed.RequestId}/reject",
            new { comment = "Rejected for reopen auth coverage" });
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);

        HttpResponseMessage reopen = await spenderClient.PostAsync(
            $"/api/v1/settlements/{reopenSeed.RequestId}/reopen", content: null);
        Assert.Equal(HttpStatusCode.OK, reopen.StatusCode);
    }

    [Fact]
    public async Task DevelopmentAuthenticationHandler_DefaultIdentity_BehavesAsSpender()
    {
        HttpClient client = _factory.CreateClient();

        // Default development identity should satisfy spender policy.
        SettlementDto created = await CreateDraftAsync(client, "Development handler behavior");
        Assert.Equal("spender.demo", created.SpenderId);

        // Same identity should fail manager-only policy at middleware layer.
        HttpResponseMessage approve = await client.PostAsync(
            $"/api/v1/settlements/{Guid.NewGuid()}/approve", content: null);
        Assert.Equal(HttpStatusCode.Forbidden, approve.StatusCode);
    }

    [Fact]
    public async Task EntraEnabled_AnonymousRequest_Returns401()
    {
        HttpClient client = _factory.CreateEntraAnonymousClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/settlements",
            new { settlementDate = "2026-07-17", purpose = "Entra anonymous" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EntraEnabled_AuthenticatedWithoutRequiredRole_Returns403()
    {
        HttpClient client = _factory.CreateEntraEnabledClient(new CurrentUser(
            UserId: "finance.contentowner.entra",
            Email: "finance.contentowner@canex.local",
            Roles: new[] { UserRole.FinanceContentOwner }));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/settlements",
            new { settlementDate = "2026-07-17", purpose = "Entra forbidden" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EntraEnabled_AuthenticatedWithSpenderRole_Succeeds()
    {
        HttpClient client = _factory.CreateEntraEnabledClient(ApiWebApplicationFactory.SpenderUser);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/settlements",
            new { settlementDate = "2026-07-17", purpose = "Entra spender success" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task BusinessBehavior_RemainsDomainDriven_AfterEndpointAuthorization()
    {
        HttpClient spenderClient = _factory.CreateClient();
        HttpClient approverClient = _factory.CreateClientWithIdentity(ApiWebApplicationFactory.ApproverUser);

        SettlementDto draft = await CreateDraftAsync(spenderClient, "Business behavior unchanged");

        // Authorized approver still hits existing domain rule (must be Submitted), proving
        // endpoint authorization did not replace or alter business-state behavior.
        HttpResponseMessage approveDraft = await approverClient.PostAsync(
            $"/api/v1/settlements/{draft.RequestId}/approve", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, approveDraft.StatusCode);
    }

    private static HttpRequestMessage CreateRequest(EndpointCase endpoint, Guid settlementId, Guid lineId)
    {
        string path = endpoint.Path
            .Replace("{settlementId}", settlementId.ToString())
            .Replace("{lineId}", lineId.ToString());

        var request = new HttpRequestMessage(endpoint.Method, path);
        if (endpoint.Body is not null)
        {
            request.Content = JsonContent.Create(endpoint.Body);
        }

        return request;
    }

    private static async Task<SettlementDto> CreateDraftAsync(HttpClient client, string purpose)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/settlements",
            new { settlementDate = "2026-07-17", purpose });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task<SettlementDto> AddLineAsync(HttpClient client, Guid settlementId, string notes)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/settlements/{settlementId}/lines",
            new
            {
                categoryCode = "OFFICE_SUPPLIES",
                grossAmount = 110m,
                isVat = false,
                notes,
                carPlate = (string?)null,
                odometerKm = (decimal?)null
            });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SettlementDto? dto = await response.Content.ReadFromJsonAsync<SettlementDto>();
        Assert.NotNull(dto);
        return dto!;
    }

    public sealed record EndpointCase(string Name, HttpMethod Method, string Path, object? Body);
}
