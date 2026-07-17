using System.Text.Json;
using Microsoft.Extensions.Options;
using PettyCash.Infrastructure.SharePoint.Configuration;
using PettyCash.Infrastructure.SharePoint.Graph;
using PettyCash.Infrastructure.SharePoint.ReferenceData.Models;

namespace PettyCash.Infrastructure.SharePoint.ReferenceData.Gateways;

internal sealed class GraphSharePointCategoryMappingsGateway : ISharePointCategoryMappingsGateway
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IGraphApiClient _graphApiClient;
    private readonly SharePointFoundationOptions _options;

    public GraphSharePointCategoryMappingsGateway(
        IGraphApiClient graphApiClient,
        IOptions<SharePointFoundationOptions> options)
    {
        _graphApiClient = graphApiClient;
        _options = options.Value;
    }

    public async Task<SharePointCategoryMappingItem?> GetByCategoryCodeAsync(string categoryCode, CancellationToken cancellationToken = default)
    {
        var escapedCategoryCode = EscapeODataString(categoryCode);
        var requestUri =
            $"sites/{_options.ReferenceData.SiteId}/lists/{_options.ReferenceData.CategoryMappingsListId}/items" +
            "?$expand=fields($select=CategoryCode,ExpenseMainAccount,DimensionDefaults,SalesTaxGroup,ItemSalesTaxGroup,KmRequired,Active)" +
            $"&$filter=fields/CategoryCode eq '{escapedCategoryCode}'" +
            "&$top=1";

        using var response = await _graphApiClient.SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, requestUri),
            operationName: "CategoryMappings.GetByCategoryCode",
            cancellationToken: cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<SharePointListItemsResponse>(stream, SerializerOptions, cancellationToken);
        var item = payload?.Value?.Select(MapToCategoryMapping).FirstOrDefault(mapped => mapped is not null);
        return item;
    }

    public async Task<IReadOnlyList<SharePointCategoryMappingItem>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var requestUri =
            $"sites/{_options.ReferenceData.SiteId}/lists/{_options.ReferenceData.CategoryMappingsListId}/items" +
            "?$expand=fields($select=CategoryCode,ExpenseMainAccount,DimensionDefaults,SalesTaxGroup,ItemSalesTaxGroup,KmRequired,Active)" +
            "&$filter=fields/Active eq true";

        using var response = await _graphApiClient.SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, requestUri),
            operationName: "CategoryMappings.GetAllActive",
            cancellationToken: cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<SharePointListItemsResponse>(stream, SerializerOptions, cancellationToken);
        if (payload?.Value is null || payload.Value.Count == 0)
        {
            return [];
        }

        return payload.Value
            .Select(MapToCategoryMapping)
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
    }

    private static SharePointCategoryMappingItem? MapToCategoryMapping(SharePointListItem item)
    {
        if (item.Fields is null)
        {
            return null;
        }

        if (!SharePointFieldReader.TryReadRequiredString(item.Fields, "CategoryCode", out var categoryCode))
        {
            return null;
        }

        if (!SharePointFieldReader.TryReadRequiredString(item.Fields, "ExpenseMainAccount", out var expenseMainAccount))
        {
            return null;
        }

        if (!SharePointFieldReader.TryReadRequiredString(item.Fields, "DimensionDefaults", out var dimensionDefaults))
        {
            return null;
        }

        if (!SharePointFieldReader.TryReadBoolean(item.Fields, "KmRequired", out var kmRequired))
        {
            kmRequired = false;
        }

        if (!SharePointFieldReader.TryReadBoolean(item.Fields, "Active", out var active))
        {
            active = false;
        }

        return new SharePointCategoryMappingItem(
            CategoryCode: categoryCode,
            ExpenseMainAccount: expenseMainAccount,
            DimensionDefaults: dimensionDefaults,
            SalesTaxGroup: SharePointFieldReader.ReadOptionalString(item.Fields, "SalesTaxGroup"),
            ItemSalesTaxGroup: SharePointFieldReader.ReadOptionalString(item.Fields, "ItemSalesTaxGroup"),
            KmRequired: kmRequired,
            Active: active);
    }

    private static string EscapeODataString(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private sealed class SharePointListItemsResponse
    {
        public List<SharePointListItem>? Value { get; init; }
    }

    private sealed class SharePointListItem
    {
        public Dictionary<string, JsonElement>? Fields { get; init; }
    }
}
