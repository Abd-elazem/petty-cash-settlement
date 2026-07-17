using System.Net;

namespace PettyCash.Infrastructure.SharePoint.Errors;

public sealed class SharePointInfrastructureException : Exception
{
    public SharePointInfrastructureException(
        string operation,
        string correlationId,
        string message,
        HttpStatusCode? statusCode = null,
        bool isTransient = false,
        TimeSpan? retryAfter = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Operation = operation;
        CorrelationId = correlationId;
        StatusCode = statusCode;
        IsTransient = isTransient;
        RetryAfter = retryAfter;
    }

    public string Operation { get; }
    public string CorrelationId { get; }
    public HttpStatusCode? StatusCode { get; }
    public bool IsTransient { get; }
    public TimeSpan? RetryAfter { get; }
}
