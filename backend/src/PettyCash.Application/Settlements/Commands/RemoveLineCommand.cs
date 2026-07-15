using FluentValidation;
using PettyCash.Application.Abstractions;
using PettyCash.Application.Authorization;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Settlements.Commands;

public sealed record RemoveLineCommand(Guid SettlementId, Guid LineId) : ICommand<SettlementDto>;

public sealed class RemoveLineCommandValidator : AbstractValidator<RemoveLineCommand>
{
    public RemoveLineCommandValidator()
    {
        RuleFor(x => x.SettlementId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
    }
}

public sealed class RemoveLineCommandHandler : ICommandHandler<RemoveLineCommand, SettlementDto>
{
    private readonly ISettlementRepository _settlements;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISettlementAuthorizationPolicy _authorization;

    public RemoveLineCommandHandler(
        ISettlementRepository settlements,
        ICurrentUserContext currentUserContext,
        ISettlementAuthorizationPolicy authorization)
    {
        _settlements = settlements;
        _currentUserContext = currentUserContext;
        _authorization = authorization;
    }

    public async Task<SettlementDto> HandleAsync(RemoveLineCommand command, CancellationToken cancellationToken = default)
    {
        var user = _currentUserContext.Current;

        var settlement = await _settlements.GetByIdAsync(command.SettlementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Settlement), command.SettlementId);

        _authorization.EnsureCanEdit(settlement, user);

        settlement.RemoveLine(command.LineId);

        await _settlements.UpdateAsync(settlement, cancellationToken);

        return SettlementMapper.ToDto(settlement);
    }
}
