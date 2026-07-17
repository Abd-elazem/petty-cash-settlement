using Microsoft.Extensions.Options;
using PettyCash.Infrastructure.SharePoint.Configuration;
using PettyCash.Infrastructure.SharePoint.Correlation;
using PettyCash.Infrastructure.SharePoint.Errors;

namespace PettyCash.Infrastructure.SharePoint.Resilience;

public sealed class SharePointRetryPolicy : ISharePointRetryPolicy
{
    private readonly SharePointFoundationOptions _options;
    private readonly ISharePointErrorMapper _errorMapper;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public SharePointRetryPolicy(
        IOptions<SharePointFoundationOptions> options,
        ISharePointErrorMapper errorMapper,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _options = options.Value;
        _errorMapper = errorMapper;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public async Task<HttpResponseMessage> ExecuteHttpAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> operation,
        string operationName,
        string? entityName = null,
        object? entityKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        Exception? lastException = null;

        for (var attempt = 1; attempt <= _options.Retry.MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var correlationId = _correlationContextAccessor.CorrelationId;

            try
            {
                var response = await operation(cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                _errorMapper.TryMapHttpFailure(
                    response,
                    operationName,
                    correlationId,
                    entityName,
                    entityKey,
                    out var mappedException,
                    out var isTransient,
                    out var retryAfter);

                if (!isTransient || attempt >= _options.Retry.MaxAttempts)
                {
                    response.Dispose();
                    throw mappedException;
                }

                response.Dispose();
                await Task.Delay(GetDelay(attempt, retryAfter), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                var mappedException = _errorMapper.MapTransportException(
                    exception,
                    operationName,
                    _correlationContextAccessor.CorrelationId);

                lastException = mappedException;

                if (!_errorMapper.IsTransientException(mappedException, out var retryAfter) || attempt >= _options.Retry.MaxAttempts)
                {
                    throw mappedException;
                }

                await Task.Delay(GetDelay(attempt, retryAfter), cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("SharePoint retry policy exhausted without producing a terminal exception.");
    }

    private TimeSpan GetDelay(int attempt, TimeSpan? retryAfter)
    {
        if (retryAfter is { } retryDelay && retryDelay > TimeSpan.Zero)
        {
            return retryDelay;
        }

        var baseDelayMs = _options.Retry.BaseDelayMilliseconds;
        var exponentialMultiplier = Math.Pow(2, Math.Max(0, attempt - 1));
        var computedDelay = (int)Math.Min(
            _options.Retry.MaxDelayMilliseconds,
            baseDelayMs * exponentialMultiplier);

        var jitter = _options.Retry.JitterMilliseconds > 0
            ? Random.Shared.Next(0, _options.Retry.JitterMilliseconds + 1)
            : 0;

        return TimeSpan.FromMilliseconds(computedDelay + jitter);
    }
}
