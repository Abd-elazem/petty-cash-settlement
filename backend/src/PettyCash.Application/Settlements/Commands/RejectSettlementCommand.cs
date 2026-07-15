using FluentValidation;
using PettyCash.Application.Abstractions;
using PettyCash.Application.Authorization;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Settlements.Commands;

public sealed record RejectSettlementCommand(Guid SettlementId, string Comment) : ICommand<SettlementDto>;

public sealed class RejectSettlementCommandValidator : AbstractValidator<RejectSettlementCommand>
{
    public RejectSettlementCommandValidator()
    {
        RuleFor(x => x.SettlementId).NotEmpty();
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(1000);
    }
}

public sealed class RejectSettlementCommandHandler : ICommandHandler<RejectSettlementCommand, SettlementDto>
{
    private readonly ISettlementRepository _settlements;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISettlementAuthorizationPolicy _authorization;
    private readonly IAuditLogger _auditLogger;

    public RejectSettlementCommandHandler(
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

    public async Task<SettlementDto> HandleAsync(RejectSettlementCommand command, CancellationToken cancellationToken = default)
    {
        var user = _currentUserContext.Current;

        var settlement = await _settlements.GetByIdAsync(command.SettlementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Settlement), command.SettlementId);

        _authorization.EnsureCanApproveOrReject(settlement, user);

        var fromStatus = settlement.Status.ToString();
        settlement.Reject(command.Comment);

        await _settlements.UpdateAsync(settlement, cancellationToken);

        await _auditLogger.LogAsync(
            new AuditLogEntry(settlement.Id, "Rejected", user.UserId, fromStatus, settlement.Status.ToString(), DateTime.UtcNow, command.Comment),
            cancellationToken);

        return SettlementMapper.ToDto(settlement);
    }
}
