using PettyCash.Application.Abstractions;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;

namespace PettyCash.Application.Settlements.Queries;

/// <summary>
/// Manager inbox (approver-scoped): only settlements currently awaiting the caller's
/// decision. Scope is derived from ICurrentUserContext; the query carries no client
/// identity input.
/// </summary>
public sealed record GetApproverInboxQuery : IQuery<IReadOnlyList<SettlementDto>>;

public sealed class GetApproverInboxQueryHandler : IQueryHandler<GetApproverInboxQuery, IReadOnlyList<SettlementDto>>
{
    private readonly ISettlementRepository _settlements;
    private readonly ICurrentUserContext _currentUserContext;

    public GetApproverInboxQueryHandler(ISettlementRepository settlements, ICurrentUserContext currentUserContext)
    {
        _settlements = settlements;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyList<SettlementDto>> HandleAsync(
        GetApproverInboxQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = _currentUserContext.Current;
        var settlements = await _settlements.GetPendingApprovalByApproverEmailAsync(user.Email, cancellationToken);
        return settlements.Select(SettlementMapper.ToDto).ToList();
    }
}
