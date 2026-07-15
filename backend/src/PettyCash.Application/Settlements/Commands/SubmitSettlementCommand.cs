using FluentValidation;
using PettyCash.Application.Abstractions;
using PettyCash.Application.Authorization;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Settlements.Commands;

public sealed record SubmitSettlementCommand(Guid SettlementId) : ICommand<SettlementDto>;

public sealed class SubmitSettlementCommandValidator : AbstractValidator<SubmitSettlementCommand>
{
    public SubmitSettlementCommandValidator()
    {
        RuleFor(x => x.SettlementId).NotEmpty();
    }
}

public sealed class SubmitSettlementCommandHandler : ICommandHandler<SubmitSettlementCommand, SettlementDto>
{
    private readonly ISettlementRepository _settlements;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISettlementAuthorizationPolicy _authorization;
    private readonly IAuditLogger _auditLogger;

    public SubmitSettlementCommandHandler(
        ISettlementRepository settlements,
        ICurrentUserContext currentUserContext,
        ISettlementAuthorizationPolicy authorization,
        IAuditLogger auditLogger)
    {
        _settlements = settlements;
        _currentUserContext = currentUserContext;
        _authorization = authorization;
        _auditLogger = auditLogger;
    }

    public async Task<SettlementDto> HandleAsync(SubmitSettlementCommand command, CancellationToken cancellationToken = default)
    {
        var user = _currentUserContext.Current;

        var settlement = await _settlements.GetByIdAsync(command.SettlementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Settlement), command.SettlementId);

        _authorization.EnsureCanSubmit(settlement, user);

        var fromStatus = settlement.Status.ToString();
        settlement.Submit();

        await _settlements.UpdateAsync(settlement, cancellationToken);

        await _auditLogger.LogAsync(
            new AuditLogEntry(settlement.Id, "Submitted", user.UserId, fromStatus, settlement.Status.ToString(), DateTime.UtcNow, null),
            cancellationToken);

        return SettlementMapper.ToDto(settlement);
    }
}
