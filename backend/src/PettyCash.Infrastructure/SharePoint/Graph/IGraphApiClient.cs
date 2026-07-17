namespace PettyCash.Infrastructure.SharePoint.Graph;

public interface IGraphApiClient
{
    Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        string operationName,
        string? entityName = null,
        object? entityKey = null,
        CancellationToken cancellationToken = default);
}
