using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PettyCash.Application.Abstractions;
using PettyCash.Infrastructure.Postgres;
using PettyCash.Infrastructure.Postgres.Repositories;

namespace PettyCash.Infrastructure.DependencyInjection;

/// <summary>
/// Composition root entry point for the dev/Postgres adapter. Swapping to the future
/// SharePoint adapter (D-002) means writing an AddSharePointInfrastructure() alongside
/// this one and changing which one the host calls — no Application or Domain code changes.
///
/// Deliberately NOT registered here: ICurrentUserContext (needs real auth — Api/Auth
/// sprint) and IPhotoStore (needs SharePoint/Graph or an object-store choice — out of
/// scope per this sprint's stop condition, and still blocked on ASSUMPTIONS.md A-016
/// regardless). A host that calls only AddApplication() + AddInfrastructure() has an
/// incomplete container for those two interfaces until a later sprint fills them in —
/// expected, not a bug.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PettyCashDev")
            ?? throw new InvalidOperationException(
                "Missing connection string 'PettyCashDev'. Add it under ConnectionStrings in configuration.");

        services.AddDbContext<PettyCashDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<ISettlementRepository, PostgresSettlementRepository>();
        services.AddScoped<ICategoryMappingRepository, PostgresCategoryMappingRepository>();
        services.AddScoped<IAppUserProfileRepository, PostgresAppUserProfileRepository>();
        services.AddScoped<IAuditLogger, PostgresAuditLogger>();
        services.AddSingleton<IVatConfiguration, ConfigurationVatConfiguration>();

        return services;
    }
}
