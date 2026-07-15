using PettyCash.Application.Abstractions;
using PettyCash.Application.Authorization;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Settlements.Queries;

public sealed record GetSettlementByIdQuery(Guid SettlementId) : IQuery<SettlementDto>;

public sealed class GetSettlementByIdQueryHandler : IQueryHandler<GetSettlementByIdQuery, SettlementDto>
{
    private readonly ISettlementRepository _settlements;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISettlementAuthorizationPolicy _authorization;

    public GetSettlementByIdQueryHandler(
        ISettlementRepository settlements,
        ICurrentUserContext currentUserContext,
        ISettlementAuthorizationPolicy authorization)
    {
        _settlements = settlements;
        _currentUserContext = currentUserContext;
        _authorization = authorization;
    }

    public async Task<SettlementDto> HandleAsync(GetSettlementByIdQuery query, CancellationToken cancellationToken = default)
    {
        var user = _currentUserContext.Current;

        var settlement = await _settlements.GetByIdAsync(query.SettlementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Settlement), query.SettlementId);

        _authorization.EnsureCanView(settlement, user);

        return SettlementMapper.ToDto(settlement);
    }
}
