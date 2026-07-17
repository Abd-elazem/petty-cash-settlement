namespace PettyCash.Infrastructure.SharePoint.Errors;

public interface ISharePointErrorMapper
{
    bool TryMapHttpFailure(
        HttpResponseMessage response,
        string operation,
        string correlationId,
        string? entityName,
        object? entityKey,
        out Exception mappedException,
        out bool isTransient,
        out TimeSpan? retryAfter);

    bool IsTransientException(Exception exception, out TimeSpan? retryAfter);

    Exception MapTransportException(Exception exception, string operation, string correlationId);
}