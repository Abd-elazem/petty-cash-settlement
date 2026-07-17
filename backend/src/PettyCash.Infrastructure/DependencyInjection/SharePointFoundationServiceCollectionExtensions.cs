using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PettyCash.Application.Abstractions;
using PettyCash.Infrastructure.SharePoint.Audit.Gateways;
using PettyCash.Infrastructure.SharePoint.Configuration;
using PettyCash.Infrastructure.SharePoint.Correlation;
using PettyCash.Infrastructure.SharePoint.Errors;
using PettyCash.Infrastructure.SharePoint.Graph;
using PettyCash.Infrastructure.SharePoint.ReferenceData.Gateways;
using PettyCash.Infrastructure.SharePoint.Repositories;
using PettyCash.Infrastructure.SharePoint.Resilience;
using PettyCash.Infrastructure.SharePoint.Settlements.Gateways;

namespace PettyCash.Infrastructure.DependencyInjection;

public static class SharePointFoundationServiceCollectionExtensions
{
    public static IServiceCollection AddSharePointFoundation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<SharePointFoundationOptions>, SharePointFoundationOptionsValidator>();
        services
            .AddOptions<SharePointFoundationOptions>()
            .Bind(configuration.GetSection(SharePointFoundationOptions.SectionName))
            .ValidateOnStart();

        services.AddHttpClient(SharePointHttpClientNames.GraphApi, (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SharePointFoundationOptions>>().Value;
            client.BaseAddress = new Uri(options.GraphApi.BaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(options.GraphApi.TimeoutSeconds);
        });

        services.AddHttpClient(SharePointHttpClientNames.GraphAuth);

        services.AddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
        services.AddSingleton<ISharePointErrorMapper, SharePointErrorMapper>();
        services.AddSingleton<ISharePointRetryPolicy, SharePointRetryPolicy>();
        services.AddSingleton<IGraphAccessTokenProvider, EntraGraphAccessTokenProvider>();
        services.AddSingleton<IGraphApiClient, GraphApiClient>();

        // SharePoint adapter registrations enabled in implemented batches.
        services.AddScoped<ISharePointCategoryMappingsGateway, GraphSharePointCategoryMappingsGateway>();
        services.AddScoped<ISharePointAppUserProfilesGateway, GraphSharePointAppUserProfilesGateway>();
        services.AddScoped<ICategoryMappingRepository, SharePointCategoryMappingRepository>();
        services.AddScoped<IAppUserProfileRepository, SharePointAppUserProfileRepository>();
        services.AddScoped<ISharePointSettlementHeadersGateway, GraphSharePointSettlementHeadersGateway>();
        services.AddScoped<ISharePointSettlementLinesGateway, GraphSharePointSettlementLinesGateway>();
        services.AddScoped<ISettlementRepository, SharePointSettlementRepository>();
        services.AddScoped<ISharePointAuditGateway, GraphSharePointAuditGateway>();
        services.AddScoped<IAuditLogger, SharePointAuditLogger>();

        return services;
    }
}
