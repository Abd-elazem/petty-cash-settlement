using PettyCash.Application.Abstractions;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;

namespace PettyCash.Application.Settlements.Queries;

/// <summary>
/// "My Settlements" (Guide §5.3): scoped to the caller by construction — there's no
/// SpenderId parameter to get wrong, and no separate authorization check is needed
/// because the repository query is already self-scoped.
/// </summary>
public sealed record GetMySettlementsQuery : IQuery<IReadOnlyList<SettlementSummaryDto>>;

public sealed class GetMySettlementsQueryHandler : IQueryHandler<GetMySettlementsQuery, IReadOnlyList<SettlementSummaryDto>>
{
    private readonly ISettlementRepository _settlements;
    private readonly ICurrentUserContext _currentUserContext;

    public GetMySettlementsQueryHandler(ISettlementRepository settlements, ICurrentUserContext currentUserContext)
    {
        _settlements = settlements;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyList<SettlementSummaryDto>> HandleAsync(GetMySettlementsQuery query, CancellationToken cancellationToken = default)
    {
        var user = _currentUserContext.Current;
        var settlements = await _settlements.GetBySpenderIdAsync(user.UserId, cancellationToken);
        return settlements.Select(SettlementMapper.ToSummaryDto).ToList();
    }
}
