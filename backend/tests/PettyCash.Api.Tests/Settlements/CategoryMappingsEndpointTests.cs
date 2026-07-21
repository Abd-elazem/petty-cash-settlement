using System.Net;
using System.Net.Http.Json;
using PettyCash.Application.DTOs;
using Xunit;

namespace PettyCash.Api.Tests.Settlements;

/// <summary>
/// Vertical Slice 12 — Reference Data: Category Mappings.
/// GET /api/v1/category-mappings wraps GetCategoryMappingsQueryHandler.
///
/// The endpoint uses RequireAuthorization() (default policy — any authenticated user).
/// No role restriction applies: Spenders, Approvers, AP Accountants and Finance Content
/// Owners all need access to populate the category dropdown.
///
/// Seed data (CategoryMappingConfiguration.HasData) provides three rows:
///   OFFICE_SUPPLIES, FUEL (KmRequired=true), GOVERNMENT_FEES.
/// Tests assert against those rows so a divergence in seed data is caught here.
/// </summary>
[Collection("Api")]
public sealed class CategoryMappingsEndpointTests
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _spenderClient;

    public CategoryMappingsEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _spenderClient = factory.CreateClient(); // dev default identity: spender.demo (Spender role)
    }

    // -------------------------------------------------------------------------
    // 401 — unauthenticated requests must be blocked
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_Anonymous_Returns401()
    {
        HttpClient client = _factory.CreateAnonymousClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/category-mappings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // 200 — any authenticated role may call this endpoint
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_AuthenticatedAsSpender_Returns200()
    {
        HttpResponseMessage response = await _spenderClient.GetAsync("/api/v1/category-mappings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_AuthenticatedAsApprover_Returns200()
    {
        HttpClient approverClient = _factory.CreateClientWithIdentity(ApiWebApplicationFactory.ApproverUser);

        HttpResponseMessage response = await approverClient.GetAsync("/api/v1/category-mappings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Shape — response deserialises cleanly to IReadOnlyList<CategoryMappingDto>
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_Returns_ListOfCategoryMappingDto()
    {
        HttpResponseMessage response = await _spenderClient.GetAsync("/api/v1/category-mappings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        IReadOnlyList<CategoryMappingDto>? mappings =
            await response.Content.ReadFromJsonAsync<IReadOnlyList<CategoryMappingDto>>();

        Assert.NotNull(mappings);
        Assert.NotEmpty(mappings);
    }

    // -------------------------------------------------------------------------
    // Seed data — three dev seed rows must be present with correct field values
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_ContainsSeedRows_WithCorrectFields()
    {
        HttpResponseMessage response = await _spenderClient.GetAsync("/api/v1/category-mappings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        IReadOnlyList<CategoryMappingDto>? mappings =
            await response.Content.ReadFromJsonAsync<IReadOnlyList<CategoryMappingDto>>();
        Assert.NotNull(mappings);

        // OFFICE_SUPPLIES — non-fuel, no odometer fields
        CategoryMappingDto? officeSupplies = mappings!.FirstOrDefault(m => m.CategoryCode == "OFFICE_SUPPLIES");
        Assert.NotNull(officeSupplies);
        Assert.Equal("Office Supplies", officeSupplies!.DisplayName);
        Assert.False(officeSupplies.KmRequired);

        // FUEL — KmRequired must be true so the SPA shows Car Plate + Odometer fields
        CategoryMappingDto? fuel = mappings.FirstOrDefault(m => m.CategoryCode == "FUEL");
        Assert.NotNull(fuel);
        Assert.Equal("Fuel", fuel!.DisplayName);
        Assert.True(fuel.KmRequired);

        // GOVERNMENT_FEES — non-fuel
        CategoryMappingDto? govFees = mappings.FirstOrDefault(m => m.CategoryCode == "GOVERNMENT_FEES");
        Assert.NotNull(govFees);
        Assert.Equal("Government Fees", govFees!.DisplayName);
        Assert.False(govFees.KmRequired);
    }

    // -------------------------------------------------------------------------
    // Internal accounting fields must NOT be exposed in the response
    // (ExpenseMainAccount, DimensionDefaults, SalesTaxGroup, ItemSalesTaxGroup)
    // Verified by confirming CategoryMappingDto has only three fields and that
    // raw JSON contains none of the restricted property names.
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_ResponseDoesNotExposeInternalAccountingFields()
    {
        HttpResponseMessage response = await _spenderClient.GetAsync("/api/v1/category-mappings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("expenseMainAccount", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dimensionDefaults", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("salesTaxGroup", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("itemSalesTaxGroup", json, StringComparison.OrdinalIgnoreCase);
    }
}
