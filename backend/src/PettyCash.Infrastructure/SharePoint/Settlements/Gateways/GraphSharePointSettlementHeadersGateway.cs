using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PettyCash.Domain.Settlements;
using PettyCash.Infrastructure.SharePoint.Configuration;
using PettyCash.Infrastructure.SharePoint.Graph;
using PettyCash.Infrastructure.SharePoint.Settlements.Models;

namespace PettyCash.Infrastructure.SharePoint.Settlements.Gateways;

internal sealed class GraphSharePointSettlementHeadersGateway : ISharePointSettlementHeadersGateway
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IGraphApiClient _graphApiClient;
    private readonly SharePointFoundationOptions _options;

    internal GraphSharePointSettlementHeadersGateway(
        IGraphApiClient graphApiClient,
        IOptions<SharePointFoundationOptions> options)
    {
        _graphApiClient = graphApiClient;
        _options = options.Value;
    }

    public async Task<SharePointSettlementHeaderItem?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var requestUri =
            $"sites/{_options.Settlements.SiteId}/lists/{_options.Settlements.SettlementHeadersListId}/items" +
            "?$expand=fields($select=RequestId,Version,SettlementDate,Purpose,SpenderId,SpenderNameSnapshot,WorkerIdSnapshot,ApproverEmailSnapshot,Status,ApprovalComment,JournalBatchNumber)" +
            $"&$filter=fields/RequestId eq '{requestId:D}'" +
            "&$top=1";

        using var response = await _graphApiClient.SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, requestUri),
            operationName: "Settlements.Headers.GetByRequestId",
            cancellationToken: cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<SharePointListItemsResponse>(stream, SerializerOptions, cancellationToken);
        return payload?.Value?.Select(MapToHeader).FirstOrDefault(item => item is not null);
    }

    public async Task<IReadOnlyList<SharePointSettlementHeaderItem>> GetBySpenderIdAsync(string spenderId, CancellationToken cancellationToken = default)
    {
        var escapedSpenderId = EscapeODataString(spenderId);
        var requestUri =
            $"sites/{_options.Settlements.SiteId}/lists/{_options.Settlements.SettlementHeadersListId}/items" +
            "?$expand=fields($select=RequestId,Version,SettlementDate,Purpose,SpenderId,SpenderNameSnapshot,WorkerIdSnapshot,ApproverEmailSnapshot,Status,ApprovalComment,JournalBatchNumber)" +
            $"&$filter=fields/SpenderId eq '{escapedSpenderId}'" +
            "&$orderby=fields/SettlementDate desc";

        using var response = await _graphApiClient.SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, requestUri),
            operationName: "Settlements.Headers.GetBySpenderId",
            cancellationToken: cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<SharePointListItemsResponse>(stream, SerializerOptions, cancellationToken);
        if (payload?.Value is null || payload.Value.Count == 0)
        {
            return [];
        }

        return payload.Value
            .Select(MapToHeader)
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
    }

    public async Task<IReadOnlyList<SharePointSettlementHeaderItem>> GetPendingApprovalByApproverEmailAsync(
        string approverEmail,
        CancellationToken cancellationToken = default)
    {
        var escapedApproverEmail = EscapeODataString(approverEmail);
        var submitted = SettlementStatus.Submitted.ToString();
        var requestUri =
            $"sites/{_options.Settlements.SiteId}/lists/{_options.Settlements.SettlementHeadersListId}/items" +
            "?$expand=fields($select=RequestId,Version,SettlementDate,Purpose,SpenderId,SpenderNameSnapshot,WorkerIdSnapshot,ApproverEmailSnapshot,Status,ApprovalComment,JournalBatchNumber)" +
            $"&$filter=fields/ApproverEmailSnapshot eq '{escapedApproverEmail}' and fields/Status eq '{submitted}'" +
            "&$orderby=fields/SettlementDate desc";

        using var response = await _graphApiClient.SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, requestUri),
            operationName: "Settlements.Headers.GetPendingApprovalByApproverEmail",
            cancellationToken: cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<SharePointListItemsResponse>(stream, SerializerOptions, cancellationToken);
        if (payload?.Value is null || payload.Value.Count == 0)
        {
            return [];
        }

        return payload.Value
            .Select(MapToHeader)
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
    }

    public async Task<SharePointSettlementHeaderItem> AddAsync(SharePointSettlementHeaderItem header, CancellationToken cancellationToken = default)
    {
        var requestUri = $"sites/{_options.Settlements.SiteId}/lists/{_options.Settlements.SettlementHeadersListId}/items";
        using var response = await _graphApiClient.SendAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = JsonContent.Create(new { fields = MapHeaderFields(header) })
                };
                return request;
            },
            operationName: "Settlements.Headers.Add",
            entityName: nameof(Settlement),
            entityKey: header.RequestId,
            cancellationToken: cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<SharePointListItem>(stream, SerializerOptions, cancellationToken);
        var mapped = payload is null ? null : MapToHeader(payload);
        if (mapped is null)
        {
            throw new InvalidOperationException("SharePoint header create response did not contain a valid settlement item.");
        }

        return mapped;
    }

    public async Task<SharePointSettlementHeaderItem> UpdateAsync(
        SharePointSettlementHeaderItem header,
        string eTag,
        CancellationToken cancellationToken = default)
    {
        var requestUri = $"sites/{_options.Settlements.SiteId}/lists/{_options.Settlements.SettlementHeadersListId}/items/{header.ItemId}";
        using var response = await _graphApiClient.SendAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
                {
                    Content = JsonContent.Create(new { fields = MapHeaderFields(header) })
                };
                request.Headers.TryAddWithoutValidation("If-Match", eTag);
                return request;
            },
            operationName: "Settlements.Headers.Update",
            entityName: nameof(Settlement),
            entityKey: header.RequestId,
            cancellationToken: cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<SharePointListItem>(stream, SerializerOptions, cancellationToken);
        var mapped = payload is null ? null : MapToHeader(payload);
        if (mapped is null)
        {
            throw new InvalidOperationException("SharePoint header update response did not contain a valid settlement item.");
        }

        return mapped;
    }

    private static Dictionary<string, object?> MapHeaderFields(SharePointSettlementHeaderItem header)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["RequestId"] = header.RequestId.ToString("D"),
            ["Version"] = header.Version,
            ["SettlementDate"] = header.SettlementDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["Purpose"] = header.Purpose,
            ["SpenderId"] = header.SpenderId,
            ["SpenderNameSnapshot"] = header.SpenderNameSnapshot,
            ["WorkerIdSnapshot"] = header.WorkerIdSnapshot,
            ["ApproverEmailSnapshot"] = header.ApproverEmailSnapshot,
            ["Status"] = header.Status,
            ["ApprovalComment"] = header.ApprovalComment,
            ["JournalBatchNumber"] = header.JournalBatchNumber
        };
    }

    private static SharePointSettlementHeaderItem? MapToHeader(SharePointListItem item)
    {
        if (item.Fields is null)
        {
            return null;
        }

        if (!TryReadGuid(item.Fields, "RequestId", out var requestId))
        {
            return null;
        }

        if (!TryReadInt(item.Fields, "Version", out var version))
        {
            return null;
        }

        if (!TryReadDateOnly(item.Fields, "SettlementDate", out var settlementDate))
        {
            return null;
        }

        if (!TryReadRequiredString(item.Fields, "Purpose", out var purpose))
        {
            return null;
        }

        if (!TryReadRequiredString(item.Fields, "SpenderId", out var spenderId))
        {
            return null;
        }

        if (!TryReadRequiredString(item.Fields, "SpenderNameSnapshot", out var spenderNameSnapshot))
        {
            return null;
        }

        if (!TryReadRequiredString(item.Fields, "WorkerIdSnapshot", out var workerIdSnapshot))
        {
            return null;
        }

        if (!TryReadRequiredString(item.Fields, "ApproverEmailSnapshot", out var approverEmailSnapshot))
        {
            return null;
        }

        if (!TryReadRequiredString(item.Fields, "Status", out var status))
        {
            return null;
        }

        return new SharePointSettlementHeaderItem(
            ItemId: item.Id ?? string.Empty,
            ETag: item.ETag,
            RequestId: requestId,
            Version: version,
            SettlementDate: settlementDate,
            Purpose: purpose,
            SpenderId: spenderId,
            SpenderNameSnapshot: spenderNameSnapshot,
            WorkerIdSnapshot: workerIdSnapshot,
            ApproverEmailSnapshot: approverEmailSnapshot,
            Status: status,
            ApprovalComment: ReadOptionalString(item.Fields, "ApprovalComment"),
            JournalBatchNumber: ReadOptionalString(item.Fields, "JournalBatchNumber"));
    }

    private static bool TryReadRequiredString(IReadOnlyDictionary<string, JsonElement> fields, string fieldName, out string value)
    {
        value = string.Empty;
        if (!TryGetField(fields, fieldName, out var element))
        {
            return false;
        }

        var candidate = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        value = candidate;
        return true;
    }

    private static string? ReadOptionalString(IReadOnlyDictionary<string, JsonElement> fields, string fieldName)
    {
        return TryGetField(fields, fieldName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private static bool TryReadInt(IReadOnlyDictionary<string, JsonElement> fields, string fieldName, out int value)
    {
        value = 0;
        if (!TryGetField(fields, fieldName, out var element))
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        return false;
    }

    private static bool TryReadGuid(IReadOnlyDictionary<string, JsonElement> fields, string fieldName, out Guid value)
    {
        value = Guid.Empty;
        if (!TryReadRequiredString(fields, fieldName, out var text))
        {
            return false;
        }

        return Guid.TryParse(text, out value);
    }

    private static bool TryReadDateOnly(IReadOnlyDictionary<string, JsonElement> fields, string fieldName, out DateOnly value)
    {
        value = default;
        if (!TryReadRequiredString(fields, fieldName, out var text))
        {
            return false;
        }

        return DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    private static bool TryGetField(IReadOnlyDictionary<string, JsonElement> fields, string fieldName, out JsonElement value)
    {
        if (fields.TryGetValue(fieldName, out value))
        {
            return true;
        }

        foreach (var pair in fields)
        {
            if (string.Equals(pair.Key, fieldName, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string EscapeODataString(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private sealed class SharePointListItemsResponse
    {
        public List<SharePointListItem>? Value { get; init; }
    }

    private sealed class SharePointListItem
    {
        public string? Id { get; init; }

        public string? ETag { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("@odata.etag")]
        public string? ODataEtag
        {
            get => ETag;
            init => ETag = value;
        }

        public Dictionary<string, JsonElement>? Fields { get; init; }
    }
}
