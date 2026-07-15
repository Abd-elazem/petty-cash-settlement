using FluentValidation;
using PettyCash.Application.Abstractions;
using PettyCash.Application.Authorization;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Exceptions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Application.Settlements.Commands;

public sealed record UpdateLineCommand(
    Guid SettlementId,
    Guid LineId,
    string CategoryCode,
    decimal GrossAmount,
    bool IsVat,
    string? Notes,
    string? CarPlate,
    decimal? OdometerKm) : ICommand<SettlementDto>;

public sealed class UpdateLineCommandValidator : AbstractValidator<UpdateLineCommand>
{
    public UpdateLineCommandValidator()
    {
        RuleFor(x => x.CategoryCode).NotEmpty();
        RuleFor(x => x.GrossAmount).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x)
            .Must(x => (x.CarPlate is null) == (x.OdometerKm is null))
            .WithMessage("Car plate and odometer reading must be provided together.");
    }
}

public sealed class UpdateLineCommandHandler : ICommandHandler<UpdateLineCommand, SettlementDto>
{
    private readonly ISettlementRepository _settlements;
    private readonly ICategoryMappingRepository _categoryMappings;
    private readonly IVatConfiguration _vatConfiguration;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISettlementAuthorizationPolicy _authorization;

    public UpdateLineCommandHandler(
        ISettlementRepository settlements,
        ICategoryMappingRepository categoryMappings,
        IVatConfiguration vatConfiguration,
        ICurrentUserContext currentUserContext,
        ISettlementAuthorizationPolicy authorization)
    {
        _settlements = settlements;
        _categoryMappings = categoryMappings;
        _vatConfiguration = vatConfiguration;
        _currentUserContext = currentUserContext;
        _authorization = authorization;
    }

    public async Task<SettlementDto> HandleAsync(UpdateLineCommand command, CancellationToken cancellationToken = default)
    {
        var user = _currentUserContext.Current;

        var settlement = await _settlements.GetByIdAsync(command.SettlementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Settlement), command.SettlementId);

        _authorization.EnsureCanEdit(settlement, user);

        var mapping = await _categoryMappings.GetByCategoryCodeAsync(command.CategoryCode, cancellationToken);
        if (mapping is null || !mapping.Active)
        {
            throw new NotFoundException(nameof(CategoryMappingReadModel), command.CategoryCode);
        }

        var odometer = command.CarPlate is not null && command.OdometerKm is not null
            ? new OdometerReading(command.CarPlate, command.OdometerKm.Value)
            : null;

        settlement.UpdateLine(
            command.LineId,
            mapping.CategoryCode,
            mapping.ExpenseMainAccount,
            mapping.DimensionDefaults,
            command.GrossAmount,
            command.IsVat,
            _vatConfiguration.CurrentRatePercent,
            mapping.KmRequired,
            odometer,
            command.Notes);

        await _settlements.UpdateAsync(settlement, cancellationToken);

        return SettlementMapper.ToDto(settlement);
    }
}
