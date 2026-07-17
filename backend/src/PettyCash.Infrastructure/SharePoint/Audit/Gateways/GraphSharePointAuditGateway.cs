using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PettyCash.Infrastructure.SharePoint.Audit.Models;
using PettyCash.Infrastructure.SharePoint.Configuration;
using PettyCash.Infrastructure.SharePoint.Graph;

namespace PettyCash.Infrastructure.SharePoint.Audit.Gateways;

internal sealed class GraphSharePointAuditGateway : ISharePointAuditGateway
{
    private readonly IGraphApiClient _graphApiClient;
    private readonly SharePointFoundationOptions _options;

    internal GraphSharePointAuditGateway(
        IGraphApiClient graphApiClient,
        IOptions<SharePointFoundationOptions> options)
    {
        _graphApiClient = graphApiClient;
        _options = options.Value;
    }

    public async Task AddAsync(SharePointAuditLogItem entry, CancellationToken cancellationToken = default)
    {
        var requestUri = $"sites/{_options.Audit.SiteId}/lists/{_options.Audit.AuditLogsListId}/items";
        using var response = await _graphApiClient.SendAsync(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = JsonContent.Create(new { fields = MapAuditFields(entry) })
                };
                return request;
            },
            operationName: "Audit.Log.Add",
            entityName: "AuditLogEntry",
            entityKey: entry.SettlementId,
            cancellationToken: cancellationToken);
    }

    private static Dictionary<string, object?> MapAuditFields(SharePointAuditLogItem entry)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["SettlementId"] = entry.SettlementId.ToString("D"),
            ["Action"] = entry.Action,
            ["PerformedByUserId"] = entry.PerformedByUserId,
            ["FromStatus"] = entry.FromStatus,
            ["ToStatus"] = entry.ToStatus,
            ["OccurredAtUtc"] = entry.OccurredAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            ["Details"] = entry.Details
        };
    }
}
