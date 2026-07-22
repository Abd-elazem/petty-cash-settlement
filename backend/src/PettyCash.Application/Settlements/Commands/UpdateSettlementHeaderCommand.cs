using FluentValidation;
using PettyCash.Application.Abstractions;
using PettyCash.Application.Authorization;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Settlements.Commands;

/// <summary>
/// VS12: Updates the settlement date and purpose of a Draft settlement.
///
/// Only the two header fields the spender authored are mutable here. Identity snapshots
/// (SpenderName, WorkerId, ApproverEmail) are fixed at creation time per Guide §5.2 and
/// are intentionally excluded from this command — IT maintains profiles, not the spender.
///
/// Frozen-layer exception D-044: Settlement.UpdateHeader() was added to the Domain
/// (private-setter fields; no existing signature changed; additive only). See DECISIONS.md.
/// </summary>
public sealed record UpdateSettlementHeaderCommand(
    Guid SettlementId,
    DateOnly SettlementDate,
    string Purpose) : ICommand<SettlementDto>;

public sealed class UpdateSettlementHeaderCommandValidator : AbstractValidator<UpdateSettlementHeaderCommand>
{
    public UpdateSettlementHeaderCommandValidator()
    {
        RuleFor(x => x.SettlementId).NotEmpty();
        RuleFor(x => x.SettlementDate).NotEqual(default(DateOnly))
            .WithMessage("Settlement date is required.");
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(500);
    }
}

public sealed class UpdateSettlementHeaderCommandHandler
    : ICommandHandler<UpdateSettlementHeaderCommand, SettlementDto>
{
    private readonly ISettlementRepository _settlements;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISettlementAuthorizationPolicy _authorization;

    public UpdateSettlementHeaderCommandHandler(
        ISettlementRepository settlements,
        ICurrentUserContext currentUserContext,
        ISettlementAuthorizationPolicy authorization)
    {
        _settlements = settlements;
        _currentUserContext = currentUserContext;
        _authorization = authorization;
    }

    public async Task<SettlementDto> HandleAsync(
        UpdateSettlementHeaderCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = _currentUserContext.Current;

        var settlement = await _settlements.GetByIdAsync(command.SettlementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Settlement), command.SettlementId);

        // EnsureCanEdit enforces: caller must be the owner AND settlement must be in Draft.
        // Rejected-status settlements are not editable here — the spender must call Reopen
        // first, which returns the settlement to Draft, before editing the header.
        _authorization.EnsureCanEdit(settlement, user);

        settlement.UpdateHeader(command.SettlementDate, command.Purpose);

        await _settlements.UpdateAsync(settlement, cancellationToken);

        return SettlementMapper.ToDto(settlement);
    }
}
