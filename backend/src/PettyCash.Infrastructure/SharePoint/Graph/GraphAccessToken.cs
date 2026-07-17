namespace PettyCash.Infrastructure.SharePoint.Graph;

public sealed record GraphAccessToken(string Value, DateTimeOffset ExpiresAtUtc)
{
    public bool IsExpired(DateTimeOffset now, TimeSpan safetyWindow) => now >= ExpiresAtUtc.Subtract(safetyWindow);
}
