using System.Text.Json;
using Microsoft.Extensions.Options;
using PettyCash.Infrastructure.SharePoint.Configuration;
using PettyCash.Infrastructure.SharePoint.Correlation;
using PettyCash.Infrastructure.SharePoint.Errors;

namespace PettyCash.Infrastructure.SharePoint.Graph;

public sealed class EntraGraphAccessTokenProvider : IGraphAccessTokenProvider
{
    private static readonly TimeSpan TokenRefreshSkew = TimeSpan.FromMinutes(2);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SharePointFoundationOptions _options;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private GraphAccessToken? _cachedToken;

    public EntraGraphAccessTokenProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<SharePointFoundationOptions> options,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public async ValueTask<GraphAccessToken> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        if (_cachedToken is not null && !_cachedToken.IsExpired(now, TokenRefreshSkew))
        {
            return _cachedToken;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            now = DateTimeOffset.UtcNow;
            if (_cachedToken is not null && !_cachedToken.IsExpired(now, TokenRefreshSkew))
            {
                return _cachedToken;
            }

            _cachedToken = _options.GraphAuth.Mode == GraphAuthMode.ManagedIdentity
                ? await GetManagedIdentityTokenAsync(cancellationToken)
                : await GetClientSecretTokenAsync(cancellationToken);

            return _cachedToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<GraphAccessToken> GetClientSecretTokenAsync(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(SharePointHttpClientNames.GraphAuth);
        var tokenEndpoint = $"https://login.microsoftonline.com/{_options.GraphAuth.TenantId}/oauth2/v2.0/token";

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.GraphAuth.ClientId,
            ["client_secret"] = _options.GraphAuth.ClientSecret!,
            ["grant_type"] = "client_credentials",
            ["scope"] = _options.GraphAuth.Scope
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint) { Content = content };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new SharePointInfrastructureException(
                operation: "AcquireGraphToken",
                correlationId: _correlationContextAccessor.CorrelationId,
                message: $"Failed to acquire Graph access token from Entra. Status={(int)response.StatusCode}.",
                statusCode: response.StatusCode,
                isTransient: false);
        }

        return ParseTokenPayload(payload);
    }

    private async Task<GraphAccessToken> GetManagedIdentityTokenAsync(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(SharePointHttpClientNames.GraphAuth);
        var query = new Dictionary<string, string>
        {
            ["api-version"] = _options.GraphAuth.ManagedIdentityApiVersion,
            ["resource"] = _options.GraphAuth.ManagedIdentityResource
        };

        if (!string.IsNullOrWhiteSpace(_options.GraphAuth.ManagedIdentityClientId))
        {
            query["client_id"] = _options.GraphAuth.ManagedIdentityClientId;
        }

        var queryString = string.Join("&", query.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        var endpoint = $"{_options.GraphAuth.ManagedIdentityEndpoint}?{queryString}";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.TryAddWithoutValidation("Metadata", "true");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new SharePointInfrastructureException(
                operation: "AcquireGraphToken",
                correlationId: _correlationContextAccessor.CorrelationId,
                message: $"Failed to acquire Graph access token via managed identity. Status={(int)response.StatusCode}.",
                statusCode: response.StatusCode,
                isTransient: false);
        }

        return ParseTokenPayload(payload);
    }

    private static GraphAccessToken ParseTokenPayload(string payload)
    {
        using var json = JsonDocument.Parse(payload);

        if (!json.RootElement.TryGetProperty("access_token", out var accessTokenElement))
        {
            throw new InvalidOperationException("Token response is missing 'access_token'.");
        }

        var accessToken = accessTokenElement.GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Token response returned an empty 'access_token'.");
        }

        DateTimeOffset expiresAtUtc;
        if (json.RootElement.TryGetProperty("expires_in", out var expiresInElement) && expiresInElement.TryGetInt32(out var expiresIn))
        {
            expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        }
        else if (json.RootElement.TryGetProperty("expires_on", out var expiresOnElement) && expiresOnElement.TryGetInt64(out var expiresOnEpoch))
        {
            expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expiresOnEpoch);
        }
        else
        {
            throw new InvalidOperationException("Token response did not include a usable expiration value.");
        }

        return new GraphAccessToken(accessToken, expiresAtUtc);
    }
}
