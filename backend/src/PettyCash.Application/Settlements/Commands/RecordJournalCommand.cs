using FluentValidation;
using PettyCash.Application.Abstractions;
using PettyCash.Application.Authorization;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Settlements.Commands;

/// <summary>
/// Called by the Power Automate flow after it creates the D365FO journal (Guide §6.2,
/// D-006). System-role only — no ownership check, since the caller is the automated
/// process, not the spender.
/// </summary>
public sealed record RecordJournalCommand(Guid SettlementId, string JournalBatchNumber) : ICommand<SettlementDto>;

public sealed class RecordJournalCommandValidator : AbstractValidator<RecordJournalCommand>
{
    public RecordJournalCommandValidator()
    {
        RuleFor(x => x.SettlementId).NotEmpty();
        RuleFor(x => x.JournalBatchNumber).NotEmpty();
    }
}

public sealed class RecordJournalCommandHandler : ICommandHandler<RecordJournalCommand, SettlementDto>
{
    private readonly ISettlementRepository _settlements;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISettlementAuthorizationPolicy _authorization;
    private readonly IAuditLogger _auditLogger;

    public RecordJournalCommandHandler(
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

    public async Task<SettlementDto> HandleAsync(RecordJournalCommand command, CancellationToken cancellationToken = default)
    {
        var user = _currentUserContext.Current;
        _authorization.EnsureCanRecordJournal(user);

        var settlement = await _settlements.GetByIdAsync(command.SettlementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Settlement), command.SettlementId);

        // Idempotency per DECISIONS.md D-008: if a retried flow run reports the same
        // journal number for a settlement already Journalled, treat it as a no-op
        // success instead of throwing InvalidSettlementStateException. A DIFFERENT
        // journal number on an already-Journalled settlement is a real conflict and
        // still throws — that's not a safe retry, it's a duplicate-journal bug upstream.
        if (settlement.Status == SettlementStatus.Journalled &&
            settlement.JournalBatchNumber == command.JournalBatchNumber)
        {
            return SettlementMapper.ToDto(settlement);
        }

        var fromStatus = settlement.Status.ToString();
        settlement.RecordJournal(command.JournalBatchNumber);

        await _settlements.UpdateAsync(settlement, cancellationToken);

        await _auditLogger.LogAsync(
            new AuditLogEntry(settlement.Id, "Journalled", user.UserId, fromStatus, settlement.Status.ToString(), DateTime.UtcNow, command.JournalBatchNumber),
            cancellationToken);

        return SettlementMapper.ToDto(settlement);
    }
}
