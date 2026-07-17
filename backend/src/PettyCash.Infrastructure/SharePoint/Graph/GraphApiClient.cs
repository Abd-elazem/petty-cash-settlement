using Microsoft.Extensions.Options;
using PettyCash.Infrastructure.SharePoint.Configuration;
using PettyCash.Infrastructure.SharePoint.Correlation;
using PettyCash.Infrastructure.SharePoint.Resilience;

namespace PettyCash.Infrastructure.SharePoint.Graph;

public sealed class GraphApiClient : IGraphApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IGraphAccessTokenProvider _accessTokenProvider;
    private readonly ISharePointRetryPolicy _retryPolicy;
    private readonly SharePointFoundationOptions _options;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public GraphApiClient(
        IHttpClientFactory httpClientFactory,
        IGraphAccessTokenProvider accessTokenProvider,
        ISharePointRetryPolicy retryPolicy,
        IOptions<SharePointFoundationOptions> options,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _httpClient = httpClientFactory.CreateClient(SharePointHttpClientNames.GraphApi);
        _accessTokenProvider = accessTokenProvider;
        _retryPolicy = retryPolicy;
        _options = options.Value;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        string operationName,
        string? entityName = null,
        object? entityKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        return _retryPolicy.ExecuteHttpAsync(
            async ct =>
            {
                var token = await _accessTokenProvider.GetAccessTokenAsync(ct);
                using var request = requestFactory();

                if (request.RequestUri is not null && !request.RequestUri.IsAbsoluteUri)
                {
                    request.RequestUri = new Uri(_httpClient.BaseAddress!, request.RequestUri);
                }

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Value);

                if (_options.Correlation.AddCorrelationHeader)
                {
                    request.Headers.Remove(_options.Correlation.HeaderName);
                    request.Headers.TryAddWithoutValidation(_options.Correlation.HeaderName, _correlationContextAccessor.CorrelationId);
                }

                return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            },
            operationName,
            entityName,
            entityKey,
            cancellationToken);
    }
}
