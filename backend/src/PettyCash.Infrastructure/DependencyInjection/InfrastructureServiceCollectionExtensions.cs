using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PettyCash.Application.Abstractions;
using PettyCash.Infrastructure.Postgres;
using PettyCash.Infrastructure.Postgres.Repositories;
using PettyCash.Infrastructure.SharePoint.Configuration;

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
        services.AddDbContext<PettyCashDbContext>((serviceProvider, options) =>
        {
            // Resolved lazily (per DbContext construction), not captured eagerly at
            // registration time. AddInfrastructure(configuration) used to read the
            // connection string once, here, into a closed-over local — which meant any
            // configuration override applied after this call (e.g. PettyCash.Api.Tests'
            // ApiWebApplicationFactory pointing ConnectionStrings:PettyCashDev at its
            // Testcontainers instance) could never take effect, since the string was
            // already baked into the AddDbContext delegate before the override was merged
            // into IConfiguration. Re-reading IConfiguration from the DI container at
            // options-configuration time fixes this without changing the config key,
            // source, or production value in any way — dev/prod still read the exact same
            // ConnectionStrings:PettyCashDev as before.
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("PettyCashDev")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'PettyCashDev'. Add it under ConnectionStrings in configuration.");

            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ISettlementRepository, PostgresSettlementRepository>();
        services.AddScoped<ICategoryMappingRepository, PostgresCategoryMappingRepository>();
        services.AddScoped<IAppUserProfileRepository, PostgresAppUserProfileRepository>();
        services.AddScoped<IAuditLogger, PostgresAuditLogger>();
        services.AddSingleton<IVatConfiguration, ConfigurationVatConfiguration>();

        // SharePoint foundation services are wired only when explicitly enabled.
        // This keeps the default local/dev Postgres behavior unchanged while allowing
        // production environments to register Graph/config/retry/error-mapping primitives
        // before SharePoint repository implementations are introduced.
        if (configuration.GetValue<bool>($"{SharePointFoundationOptions.SectionName}:Enabled"))
        {
            services.AddSharePointFoundation(configuration);
        }

        return services;
    }
}
