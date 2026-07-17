using System.Text.Json;
using Microsoft.Extensions.Options;
using PettyCash.Infrastructure.SharePoint.Configuration;
using PettyCash.Infrastructure.SharePoint.Graph;
using PettyCash.Infrastructure.SharePoint.ReferenceData.Models;

namespace PettyCash.Infrastructure.SharePoint.ReferenceData.Gateways;

internal sealed class GraphSharePointAppUserProfilesGateway : ISharePointAppUserProfilesGateway
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IGraphApiClient _graphApiClient;
    private readonly SharePointFoundationOptions _options;

    public GraphSharePointAppUserProfilesGateway(
        IGraphApiClient graphApiClient,
        IOptions<SharePointFoundationOptions> options)
    {
        _graphApiClient = graphApiClient;
        _options = options.Value;
    }

    public async Task<SharePointAppUserProfileItem?> GetByAppUserIdAsync(string appUserId, CancellationToken cancellationToken = default)
    {
        var escapedAppUserId = EscapeODataString(appUserId);
        var requestUri =
            $"sites/{_options.ReferenceData.SiteId}/lists/{_options.ReferenceData.AppUserProfilesListId}/items" +
            "?$expand=fields($select=AppUserId,DisplayName,WorkerId,ApproverEmail,Active)" +
            $"&$filter=fields/AppUserId eq '{escapedAppUserId}'" +
            "&$top=1";

        using var response = await _graphApiClient.SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, requestUri),
            operationName: "AppUserProfiles.GetByAppUserId",
            cancellationToken: cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<SharePointListItemsResponse>(stream, SerializerOptions, cancellationToken);
        var item = payload?.Value?.Select(MapToAppUserProfile).FirstOrDefault(mapped => mapped is not null);
        return item;
    }

    private static SharePointAppUserProfileItem? MapToAppUserProfile(SharePointListItem item)
    {
        if (item.Fields is null)
        {
            return null;
        }

        if (!SharePointFieldReader.TryReadRequiredString(item.Fields, "AppUserId", out var appUserId))
        {
            return null;
        }

        if (!SharePointFieldReader.TryReadRequiredString(item.Fields, "DisplayName", out var displayName))
        {
            return null;
        }

        if (!SharePointFieldReader.TryReadRequiredString(item.Fields, "WorkerId", out var workerId))
        {
            return null;
        }

        if (!SharePointFieldReader.TryReadRequiredString(item.Fields, "ApproverEmail", out var approverEmail))
        {
            return null;
        }

        if (!SharePointFieldReader.TryReadBoolean(item.Fields, "Active", out var active))
        {
            active = false;
        }

        return new SharePointAppUserProfileItem(
            AppUserId: appUserId,
            DisplayName: displayName,
            WorkerId: workerId,
            ApproverEmail: approverEmail,
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
