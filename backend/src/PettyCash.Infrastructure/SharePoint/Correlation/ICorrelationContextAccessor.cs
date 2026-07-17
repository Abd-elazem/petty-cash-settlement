namespace PettyCash.Infrastructure.SharePoint.Correlation;

public interface ICorrelationContextAccessor
{
    string CorrelationId { get; }
    IDisposable Push(string correlationId);
}
