using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PettyCash.Domain.Settlements;
using PettyCash.Infrastructure.SharePoint.Configuration;
using PettyCash.Infrastructure.SharePoint.Graph;
using PettyCash.Infrastructure.SharePoint.Settlements.Models;

namespace PettyCash.Infrastructure.SharePoint.Settlements.Gateways;

internal sealed class GraphSharePointSettlementLinesGateway : ISharePointSettlementLinesGateway
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IGraphApiClient _graphApiClient;
    private readonly SharePointFoundationOptions _options;

    internal GraphSharePointSettlementLinesGateway(
        IGraphApiClient graphApiClient,
        IOptions<SharePointFoundationOptions> options)
    {
        _graphApiClient = graphApiClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<SharePointSettlementLineItem>> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var requestUri =
            $"sites/{_options.Settlements.SiteId}/lists/{_options.Settlements.SettlementLinesListId}/items" +
            "?$expand=fields($select=RequestId,LineId,LineNo,CategoryCode,ExpenseMainAccountSnapshot,DimensionDefaultsSnapshot,GrossAmount,Currency,IsVat,VatAmount,NetAmount,Notes,CarPlate,OdometerKm)" +
            $"&$filter=fields/RequestId eq '{requestId:D}'" +
            "&$orderby=fields/LineNo asc";

        using var response = await _graphApiClient.SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, requestUri),
            operationName: "Settlements.Lines.GetByRequestId",
            cancellationToken: cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<SharePointListItemsResponse>(stream, SerializerOptions, cancellationToken);
        if (payload?.Value is null || payload.Value.Count == 0)
        {
            return [];
        }

        return payload.Value
            .Select(MapToLine)
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderBy(item => item.LineNo)
            .ToList();
    }

    public async Task ReplaceForSettlementAsync(
        Guid requestId,
        IReadOnlyList<SharePointSettlementLineItem> lines,
        CancellationToken cancellationToken = default)
    {
        var existingLines = await GetByRequestIdAsync(requestId, cancellationToken);
        foreach (var existing in existingLines)
        {
            var deleteUri = $"sites/{_options.Settlements.SiteId}/lists/{_options.Settlements.SettlementLinesListId}/items/{existing.ItemId}";
            using var deleteResponse = await _graphApiClient.SendAsync(
                () => new HttpRequestMessage(HttpMethod.Delete, deleteUri),
                operationName: "Settlements.Lines.Delete",
                entityName: nameof(Settlement),
                entityKey: requestId,
                cancellationToken: cancellationToken);
        }

        foreach (var line in lines.OrderBy(l => l.LineNo))
        {
            var createUri = $"sites/{_options.Settlements.SiteId}/lists/{_options.Settlements.SettlementLinesListId}/items";
            using var createResponse = await _graphApiClient.SendAsync(
                () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, createUri)
                    {
                        Content = JsonContent.Create(new { fields = MapLineFields(line) })
                    };
                    return request;
                },
                operationName: "Settlements.Lines.Add",
                entityName: nameof(Settlement),
                entityKey: requestId,
                cancellationToken: cancellationToken);
        }
    }

    private static Dictionary<string, object?> MapLineFields(SharePointSettlementLineItem line)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["RequestId"] = line.RequestId.ToString("D"),
            ["LineId"] = line.LineId.ToString("D"),
            ["LineNo"] = line.LineNo,
            ["CategoryCode"] = line.CategoryCode,
            ["ExpenseMainAccountSnapshot"] = line.ExpenseMainAccountSnapshot,
            ["DimensionDefaultsSnapshot"] = line.DimensionDefaultsSnapshot,
            ["GrossAmount"] = line.GrossAmount,
            ["Currency"] = line.Currency,
            ["IsVat"] = line.IsVat,
            ["VatAmount"] = line.VatAmount,
            ["NetAmount"] = line.NetAmount,
            ["Notes"] = line.Notes,
            ["CarPlate"] = line.CarPlate,
            ["OdometerKm"] = line.OdometerKm
        };
    }

    private static SharePointSettlementLineItem? MapToLine(SharePointListItem item)
    {
        if (item.Fields is null)
        {
            return null;
        }

        if (!TryReadGuid(item.Fields, "RequestId", out var requestId))
        {
            return null;
        }

        if (!TryReadGuid(item.Fields, "LineId", out var lineId))
        {
            return null;
        }

        if (!TryReadInt(item.Fields, "LineNo", out var lineNo))
        {
            return null;
        }

        if (!TryReadRequiredString(item.Fields, "CategoryCode", out var categoryCode))
        {
            return null;
        }

        if (!TryReadRequiredString(item.Fields, "ExpenseMainAccountSnapshot", out var expenseMainAccountSnapshot))
        {
            return null;
        }

        if (!TryReadRequiredString(item.Fields, "DimensionDefaultsSnapshot", out var dimensionDefaultsSnapshot))
        {
            return null;
        }

        if (!TryReadDecimal(item.Fields, "GrossAmount", out var grossAmount))
        {
            return null;
        }

        var currency = ReadOptionalString(item.Fields, "Currency") ?? "EGP";

        if (!TryReadBoolean(item.Fields, "IsVat", out var isVat))
        {
            isVat = false;
        }

        if (!TryReadDecimal(item.Fields, "VatAmount", out var vatAmount))
        {
            vatAmount = 0m;
        }

        if (!TryReadDecimal(item.Fields, "NetAmount", out var netAmount))
        {
            netAmount = grossAmount;
        }

        return new SharePointSettlementLineItem(
            ItemId: item.Id ?? string.Empty,
            ETag: item.ETag,
            RequestId: requestId,
            LineId: lineId,
            LineNo: lineNo,
            CategoryCode: categoryCode,
            ExpenseMainAccountSnapshot: expenseMainAccountSnapshot,
            DimensionDefaultsSnapshot: dimensionDefaultsSnapshot,
            GrossAmount: grossAmount,
            Currency: currency,
            IsVat: isVat,
            VatAmount: vatAmount,
            NetAmount: netAmount,
            Notes: ReadOptionalString(item.Fields, "Notes"),
            CarPlate: ReadOptionalString(item.Fields, "CarPlate"),
            OdometerKm: ReadOptionalDecimal(item.Fields, "OdometerKm"));
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

    private static bool TryReadGuid(IReadOnlyDictionary<string, JsonElement> fields, string fieldName, out Guid value)
    {
        value = Guid.Empty;
        return TryReadRequiredString(fields, fieldName, out var text) && Guid.TryParse(text, out value);
    }

    private static bool TryReadBoolean(IReadOnlyDictionary<string, JsonElement> fields, string fieldName, out bool value)
    {
        value = false;
        if (!TryGetField(fields, fieldName, out var element))
        {
            return false;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.True:
                value = true;
                return true;
            case JsonValueKind.False:
                value = false;
                return true;
            case JsonValueKind.Number when element.TryGetInt32(out var intValue):
                value = intValue != 0;
                return true;
            case JsonValueKind.String:
                var text = element.GetString();
                if (bool.TryParse(text, out var boolValue))
                {
                    value = boolValue;
                    return true;
                }

                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stringInt))
                {
                    value = stringInt != 0;
                    return true;
                }

                return false;
            default:
                return false;
        }
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

        return element.ValueKind == JsonValueKind.String &&
               int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryReadDecimal(IReadOnlyDictionary<string, JsonElement> fields, string fieldName, out decimal value)
    {
        value = 0m;
        if (!TryGetField(fields, fieldName, out var element))
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out value))
        {
            return true;
        }

        return element.ValueKind == JsonValueKind.String &&
               decimal.TryParse(element.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static decimal? ReadOptionalDecimal(IReadOnlyDictionary<string, JsonElement> fields, string fieldName)
    {
        return TryReadDecimal(fields, fieldName, out var value) ? value : null;
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
