using PettyCash.Application.Abstractions;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;
using PettyCash.Infrastructure.SharePoint.Settlements.Gateways;
using PettyCash.Infrastructure.SharePoint.Settlements.Mapping;

namespace PettyCash.Infrastructure.SharePoint.Repositories;

internal sealed class SharePointSettlementRepository : ISettlementRepository
{
    private readonly ISharePointSettlementHeadersGateway _headersGateway;
    private readonly ISharePointSettlementLinesGateway _linesGateway;
    private readonly Dictionary<Guid, SettlementConcurrencyToken> _trackedTokens = [];

    internal SharePointSettlementRepository(
        ISharePointSettlementHeadersGateway headersGateway,
        ISharePointSettlementLinesGateway linesGateway)
    {
        _headersGateway = headersGateway;
        _linesGateway = linesGateway;
    }

    public async Task<Settlement?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var header = await _headersGateway.GetByRequestIdAsync(requestId, cancellationToken);
        if (header is null)
        {
            return null;
        }

        var lines = await _linesGateway.GetByRequestIdAsync(requestId, cancellationToken);
        var settlement = SettlementHydration.CreateAggregate(header, lines);
        TrackToken(settlement.Id, header.ItemId, header.ETag);
        return settlement;
    }

    public async Task<IReadOnlyList<Settlement>> GetBySpenderIdAsync(string spenderId, CancellationToken cancellationToken = default)
    {
        var headers = await _headersGateway.GetBySpenderIdAsync(spenderId, cancellationToken);
        if (headers.Count == 0)
        {
            return [];
        }

        var result = new List<Settlement>(headers.Count);
        foreach (var header in headers)
        {
            var lines = await _linesGateway.GetByRequestIdAsync(header.RequestId, cancellationToken);
            var settlement = SettlementHydration.CreateAggregate(header, lines);
            TrackToken(settlement.Id, header.ItemId, header.ETag);
            result.Add(settlement);
        }

        return result
            .OrderByDescending(s => s.SettlementDate)
            .ToList();
    }

    public async Task AddAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        var header = SettlementHydration.ToHeader(settlement, itemId: string.Empty, eTag: null);
        var createdHeader = await _headersGateway.AddAsync(header, cancellationToken);
        var lines = SettlementHydration.ToLines(settlement);
        await _linesGateway.ReplaceForSettlementAsync(settlement.Id, lines, cancellationToken);
        TrackToken(settlement.Id, createdHeader.ItemId, createdHeader.ETag);
    }

    public async Task UpdateAsync(Settlement settlement, CancellationToken cancellationToken = default)
    {
        var token = await ResolveTokenAsync(settlement.Id, cancellationToken);
        if (string.IsNullOrWhiteSpace(token.ETag))
        {
            throw new ConcurrencyException(nameof(Settlement), settlement.Id);
        }

        var header = SettlementHydration.ToHeader(settlement, token.ItemId, token.ETag);
        var updatedHeader = await _headersGateway.UpdateAsync(header, token.ETag, cancellationToken);
        var lines = SettlementHydration.ToLines(settlement);
        await _linesGateway.ReplaceForSettlementAsync(settlement.Id, lines, cancellationToken);

        TrackToken(settlement.Id, updatedHeader.ItemId, updatedHeader.ETag);
    }

    private async Task<SettlementConcurrencyToken> ResolveTokenAsync(Guid requestId, CancellationToken cancellationToken)
    {
        if (_trackedTokens.TryGetValue(requestId, out var token))
        {
            return token;
        }

        var header = await _headersGateway.GetByRequestIdAsync(requestId, cancellationToken);
        if (header is null)
        {
            throw new NotFoundException(nameof(Settlement), requestId);
        }

        var resolvedToken = new SettlementConcurrencyToken(header.ItemId, header.ETag);
        _trackedTokens[requestId] = resolvedToken;
        return resolvedToken;
    }

    private void TrackToken(Guid requestId, string itemId, string? eTag)
    {
        _trackedTokens[requestId] = new SettlementConcurrencyToken(itemId, eTag);
    }

    private sealed record SettlementConcurrencyToken(string ItemId, string? ETag);
}
