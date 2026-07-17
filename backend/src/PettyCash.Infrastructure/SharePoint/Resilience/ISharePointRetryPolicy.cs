namespace PettyCash.Infrastructure.SharePoint.Resilience;

public interface ISharePointRetryPolicy
{
    Task<HttpResponseMessage> ExecuteHttpAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> operation,
        string operationName,
        string? entityName = null,
        object? entityKey = null,
        CancellationToken cancellationToken = default);
}
