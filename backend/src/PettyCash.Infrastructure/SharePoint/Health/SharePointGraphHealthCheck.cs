using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PettyCash.Infrastructure.SharePoint.Configuration;
using PettyCash.Infrastructure.SharePoint.Graph;

namespace PettyCash.Infrastructure.SharePoint.Health;

public sealed class SharePointGraphHealthCheck : IHealthCheck
{
    private readonly IGraphApiClient _graphApiClient;
    private readonly SharePointFoundationOptions _options;

    public SharePointGraphHealthCheck(
        IGraphApiClient graphApiClient,
        IOptions<SharePointFoundationOptions> options)
    {
        _graphApiClient = graphApiClient;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Mirror the active SharePoint repository path by validating Graph access
        // against the same configured settlements site id.
        string requestUri = $"sites/{_options.Settlements.SiteId}?$select=id";

        try
        {
            using var response = await _graphApiClient.SendAsync(
                () => new HttpRequestMessage(HttpMethod.Get, requestUri),
                operationName: "HealthCheck.SharePointGraph",
                entityName: "SharePointSite",
                entityKey: _options.Settlements.SiteId,
                cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("SharePoint Graph connectivity is healthy.");
            }

            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
            {
                ["statusCode"] = (int)response.StatusCode,
                ["responseBody"] = body
            };
            return HealthCheckResult.Unhealthy(
                $"SharePoint Graph health probe failed with status {(int)response.StatusCode}.",
                data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SharePoint Graph health probe threw an exception.", ex);
        }
    }
}
