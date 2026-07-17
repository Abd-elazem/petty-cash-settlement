using System.Net;
using PettyCash.Application.Exceptions;

namespace PettyCash.Infrastructure.SharePoint.Errors;

public sealed class SharePointErrorMapper : ISharePointErrorMapper
{
    public bool TryMapHttpFailure(
        HttpResponseMessage response,
        string operation,
        string correlationId,
        string? entityName,
        object? entityKey,
        out Exception mappedException,
        out bool isTransient,
        out TimeSpan? retryAfter)
    {
        var statusCode = response.StatusCode;
        retryAfter = response.Headers.RetryAfter?.Delta;

        switch (statusCode)
        {
            case HttpStatusCode.NotFound when !string.IsNullOrWhiteSpace(entityName) && entityKey is not null:
                mappedException = new NotFoundException(entityName!, entityKey);
                isTransient = false;
                return true;
            case HttpStatusCode.NotFound:
                mappedException = CreateInfrastructureException(
                    operation,
                    correlationId,
                    "SharePoint resource was not found.",
                    statusCode,
                    isTransient: false,
                    retryAfter: null);
                isTransient = false;
                return true;
            case HttpStatusCode.PreconditionFailed:
            case HttpStatusCode.Conflict:
                mappedException = !string.IsNullOrWhiteSpace(entityName) && entityKey is not null
                    ? new ConcurrencyException(entityName!, entityKey)
                    : CreateInfrastructureException(
                        operation,
                        correlationId,
                        "SharePoint concurrency conflict encountered.",
                        statusCode,
                        isTransient: false,
                        retryAfter: null);
                isTransient = false;
                return true;
            case HttpStatusCode.Forbidden:
                mappedException = new ForbiddenException("Access to SharePoint was denied.");
                isTransient = false;
                return true;
            case HttpStatusCode.Unauthorized:
                mappedException = CreateInfrastructureException(
                    operation,
                    correlationId,
                    "SharePoint authentication failed.",
                    statusCode,
                    isTransient: false,
                    retryAfter: null);
                isTransient = false;
                return true;
            case HttpStatusCode.TooManyRequests:
                mappedException = CreateInfrastructureException(
                    operation,
                    correlationId,
                    "SharePoint request was throttled.",
                    statusCode,
                    isTransient: true,
                    retryAfter: retryAfter);
                isTransient = true;
                return true;
            case HttpStatusCode.BadGateway:
            case HttpStatusCode.ServiceUnavailable:
            case HttpStatusCode.GatewayTimeout:
            case HttpStatusCode.InternalServerError:
                mappedException = CreateInfrastructureException(
                    operation,
                    correlationId,
                    "SharePoint service returned a transient server error.",
                    statusCode,
                    isTransient: true,
                    retryAfter: retryAfter);
                isTransient = true;
                return true;
            default:
                mappedException = CreateInfrastructureException(
                    operation,
                    correlationId,
                    $"SharePoint request failed with status {(int)statusCode} ({statusCode}).",
                    statusCode,
                    isTransient: false,
                    retryAfter: null);
                isTransient = false;
                return true;
        }
    }

    public bool IsTransientException(Exception exception, out TimeSpan? retryAfter)
    {
        retryAfter = null;

        if (exception is SharePointInfrastructureException infrastructureException)
        {
            retryAfter = infrastructureException.RetryAfter;
            return infrastructureException.IsTransient;
        }

        if (exception is HttpRequestException)
        {
            return true;
        }

        if (exception is TaskCanceledException)
        {
            return true;
        }

        return false;
    }

    public Exception MapTransportException(Exception exception, string operation, string correlationId)
    {
        if (exception is SharePointInfrastructureException or AppException)
        {
            return exception;
        }

        var transient = exception is HttpRequestException or TaskCanceledException;
        var message = transient
            ? "SharePoint request failed due to a transient transport error."
            : "SharePoint request failed due to an infrastructure error.";

        return CreateInfrastructureException(
            operation,
            correlationId,
            message,
            statusCode: null,
            isTransient: transient,
            retryAfter: null,
            innerException: exception);
    }

    private static SharePointInfrastructureException CreateInfrastructureException(
        string operation,
        string correlationId,
        string message,
        HttpStatusCode? statusCode,
        bool isTransient,
        TimeSpan? retryAfter,
        Exception? innerException = null)
    {
        return new SharePointInfrastructureException(
            operation,
            correlationId,
            $"{message} CorrelationId={correlationId}.",
            statusCode,
            isTransient,
            retryAfter,
            innerException);
    }
}
