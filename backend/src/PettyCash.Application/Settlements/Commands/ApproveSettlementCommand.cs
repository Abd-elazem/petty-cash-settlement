using FluentValidation;
using PettyCash.Application.Abstractions;
using PettyCash.Application.Authorization;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Settlements.Commands;

public sealed record ApproveSettlementCommand(Guid SettlementId) : ICommand<SettlementDto>;

public sealed class ApproveSettlementCommandValidator : AbstractValidator<ApproveSettlementCommand>
{
    public ApproveSettlementCommandValidator()
    {
        RuleFor(x => x.SettlementId).NotEmpty();
    }
}

public sealed class ApproveSettlementCommandHandler : ICommandHandler<ApproveSettlementCommand, SettlementDto>
{
    private readonly ISettlementRepository _settlements;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISettlementAuthorizationPolicy _authorization;
    private readonly IAuditLogger _auditLogger;

    public ApproveSettlementCommandHandler(
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

    public async Task<SettlementDto> HandleAsync(ApproveSettlementCommand command, CancellationToken cancellationToken = default)
    {
        var user = _currentUserContext.Current;

        var settlement = await _settlements.GetByIdAsync(command.SettlementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Settlement), command.SettlementId);

        _authorization.EnsureCanApproveOrReject(settlement, user);

        var fromStatus = settlement.Status.ToString();
        settlement.Approve();

        await _settlements.UpdateAsync(settlement, cancellationToken);

        await _auditLogger.LogAsync(
            new AuditLogEntry(settlement.Id, "Approved", user.UserId, fromStatus, settlement.Status.ToString(), DateTime.UtcNow, null),
            cancellationToken);

        return SettlementMapper.ToDto(settlement);
    }
}
