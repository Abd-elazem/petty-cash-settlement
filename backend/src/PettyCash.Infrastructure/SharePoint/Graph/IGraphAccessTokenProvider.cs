namespace PettyCash.Infrastructure.SharePoint.Graph;

public interface IGraphAccessTokenProvider
{
    ValueTask<GraphAccessToken> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
