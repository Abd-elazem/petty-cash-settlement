using FluentValidation;
using PettyCash.Application.Abstractions;
using PettyCash.Application.Authorization;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Settlements.Commands;

/// <summary>
/// Deliberately does NOT take a SpenderId parameter — the spender is always the caller,
/// resolved from ICurrentUserContext. Accepting an identity field on the command would
/// let a malicious or buggy client create a settlement "as" someone else; the server is
/// the only source of truth for who is making the request.
/// </summary>
public sealed record CreateDraftSettlementCommand(
    DateOnly SettlementDate,
    string Purpose) : ICommand<SettlementDto>;

public sealed class CreateDraftSettlementCommandValidator : AbstractValidator<CreateDraftSettlementCommand>
{
    public CreateDraftSettlementCommandValidator()
    {
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(500);
        RuleFor(x => x.SettlementDate).NotEqual(default(DateOnly));
    }
}

public sealed class CreateDraftSettlementCommandHandler
    : ICommandHandler<CreateDraftSettlementCommand, SettlementDto>
{
    private readonly ISettlementRepository _settlements;
    private readonly IAppUserProfileRepository _userProfiles;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISettlementAuthorizationPolicy _authorization;
    private readonly IAuditLogger _auditLogger;

    public CreateDraftSettlementCommandHandler(
        ISettlementRepository settlements,
        IAppUserProfileRepository userProfiles,
        ICurrentUserContext currentUserContext,
        ISettlementAuthorizationPolicy authorization,
        IAuditLogger auditLogger)
    {
        _settlements = settlements;
        _userProfiles = userProfiles;
        _currentUserContext = currentUserContext;
        _authorization = authorization;
        _auditLogger = auditLogger;
    }

    public async Task<SettlementDto> HandleAsync(CreateDraftSettlementCommand command, CancellationToken cancellationToken = default)
    {
        var user = _currentUserContext.Current;
        _authorization.EnsureCanCreate(user);

        var profile = await _userProfiles.GetByAppUserIdAsync(user.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUserProfileReadModel), user.UserId);

        if (!profile.Active)
        {
            throw new ForbiddenException("This user account is deactivated and cannot create settlements.");
        }

        var settlement = Settlement.CreateDraft(
            profile.AppUserId,
            profile.DisplayName,
            profile.WorkerId,
            profile.ApproverEmail,
            command.SettlementDate,
            command.Purpose);

        await _settlements.AddAsync(settlement, cancellationToken);

        await _auditLogger.LogAsync(
            new AuditLogEntry(settlement.Id, "Created", user.UserId, null, settlement.Status.ToString(), DateTime.UtcNow, null),
            cancellationToken);

        return SettlementMapper.ToDto(settlement);
    }
}
