using FluentValidation;
using PettyCash.Application.Abstractions;
using PettyCash.Application.Authorization;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Settlements.Commands;

/// <summary>Rejected → Draft, per DECISIONS.md D-014. Only the owning spender may reopen.</summary>
public sealed record ReopenSettlementCommand(Guid SettlementId) : ICommand<SettlementDto>;

public sealed class ReopenSettlementCommandValidator : AbstractValidator<ReopenSettlementCommand>
{
    public ReopenSettlementCommandValidator()
    {
        RuleFor(x => x.SettlementId).NotEmpty();
    }
}

public sealed class ReopenSettlementCommandHandler : ICommandHandler<ReopenSettlementCommand, SettlementDto>
{
    private readonly ISettlementRepository _settlements;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISettlementAuthorizationPolicy _authorization;
    private readonly IAuditLogger _auditLogger;

    public ReopenSettlementCommandHandler(
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

    public async Task<SettlementDto> HandleAsync(ReopenSettlementCommand command, CancellationToken cancellationToken = default)
    {
        var user = _currentUserContext.Current;

        var settlement = await _settlements.GetByIdAsync(command.SettlementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Settlement), command.SettlementId);

        _authorization.EnsureCanReopen(settlement, user);

        var fromStatus = settlement.Status.ToString();
        settlement.ReopenForEdit();

        await _settlements.UpdateAsync(settlement, cancellationToken);

        await _auditLogger.LogAsync(
            new AuditLogEntry(settlement.Id, "ReopenedForEdit", user.UserId, fromStatus, settlement.Status.ToString(), DateTime.UtcNow, $"Version {settlement.Version}"),
            cancellationToken);

        return SettlementMapper.ToDto(settlement);
    }
}
