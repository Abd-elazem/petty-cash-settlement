using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PettyCash.Application.Authorization;
using PettyCash.Application.Common;
using PettyCash.Application.DTOs;
using PettyCash.Application.Settlements.Commands;
using PettyCash.Application.Settlements.Queries;

namespace PettyCash.Application.DependencyInjection;

/// <summary>
/// Composition root entry point for this layer. Infrastructure/Api calls AddApplication()
/// once at startup; it never needs to know the individual handler types. Only depends on
/// Microsoft.Extensions.DependencyInjection.Abstractions (an interfaces-only package),
/// not on ASP.NET Core itself.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISettlementAuthorizationPolicy, SettlementAuthorizationPolicy>();

        services.AddScoped<ICommandHandler<CreateDraftSettlementCommand, SettlementDto>, CreateDraftSettlementCommandHandler>();
        services.AddScoped<ICommandHandler<AddLineCommand, SettlementDto>, AddLineCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateLineCommand, SettlementDto>, UpdateLineCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveLineCommand, SettlementDto>, RemoveLineCommandHandler>();
        services.AddScoped<ICommandHandler<SubmitSettlementCommand, SettlementDto>, SubmitSettlementCommandHandler>();
        services.AddScoped<ICommandHandler<ApproveSettlementCommand, SettlementDto>, ApproveSettlementCommandHandler>();
        services.AddScoped<ICommandHandler<RejectSettlementCommand, SettlementDto>, RejectSettlementCommandHandler>();
        services.AddScoped<ICommandHandler<ReopenSettlementCommand, SettlementDto>, ReopenSettlementCommandHandler>();
        services.AddScoped<ICommandHandler<RecordJournalCommand, SettlementDto>, RecordJournalCommandHandler>();

        services.AddScoped<IQueryHandler<GetSettlementByIdQuery, SettlementDto>, GetSettlementByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetMySettlementsQuery, IReadOnlyList<SettlementSummaryDto>>, GetMySettlementsQueryHandler>();

        services.AddValidatorsFromAssemblyContaining<CreateDraftSettlementCommandValidator>();

        return services;
    }
}
