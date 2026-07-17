using System.Diagnostics;

namespace PettyCash.Infrastructure.SharePoint.Correlation;

public sealed class CorrelationContextAccessor : ICorrelationContextAccessor
{
    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

    public string CorrelationId
    {
        get
        {
            CurrentCorrelationId.Value ??= ResolveCorrelationId();
            return CurrentCorrelationId.Value;
        }
    }

    public IDisposable Push(string correlationId)
    {
        var previous = CurrentCorrelationId.Value;
        CurrentCorrelationId.Value = string.IsNullOrWhiteSpace(correlationId) ? ResolveCorrelationId() : correlationId.Trim();
        return new RestoreScope(previous);
    }

    private static string ResolveCorrelationId()
    {
        if (Activity.Current is { } activity)
        {
            if (activity.TraceId != default)
            {
                return activity.TraceId.ToString();
            }

            if (!string.IsNullOrWhiteSpace(activity.Id))
            {
                return activity.Id!;
            }
        }

        return Guid.NewGuid().ToString("N");
    }

    private sealed class RestoreScope : IDisposable
    {
        private readonly string? _previous;
        private bool _disposed;

        public RestoreScope(string? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CurrentCorrelationId.Value = _previous;
            _disposed = true;
        }
    }
}
