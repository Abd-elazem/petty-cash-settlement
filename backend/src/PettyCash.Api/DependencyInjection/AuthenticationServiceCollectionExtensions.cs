using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PettyCash.Api.Development;
using PettyCash.Application.Abstractions;
using PettyCash.Infrastructure.Authentication;

namespace PettyCash.Api.DependencyInjection;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationFoundation(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSingleton<IValidateOptions<EntraAuthenticationOptions>, EntraAuthenticationOptionsValidator>();
        services
            .AddOptions<EntraAuthenticationOptions>()
            .Bind(configuration.GetSection(EntraAuthenticationOptions.SectionName))
            .ValidateOnStart();

        EntraAuthenticationOptions entraOptions = configuration
            .GetSection(EntraAuthenticationOptions.SectionName)
            .Get<EntraAuthenticationOptions>() ?? new EntraAuthenticationOptions();

        if (!entraOptions.Enabled)
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Authentication:Entra:Enabled must be true outside Development.");
            }

            services.AddHttpContextAccessor();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = DevelopmentAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = DevelopmentAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                    DevelopmentAuthenticationHandler.SchemeName,
                    _ => { });

            services.AddScoped<ICurrentUserContext, DevelopmentCurrentUserContext>();
            AuthorizationBuilder developmentAuthorization = services.AddAuthorizationBuilder();
            developmentAuthorization.SetDefaultPolicy(
                new AuthorizationPolicyBuilder(DevelopmentAuthenticationHandler.SchemeName)
                    .RequireAuthenticatedUser()
                    .Build());
            ConfigureRolePolicies(developmentAuthorization);
            return services;
        }

        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        string authority = string.IsNullOrWhiteSpace(entraOptions.Authority)
            ? $"https://login.microsoftonline.com/{entraOptions.TenantId}/v2.0"
            : entraOptions.Authority;

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, ClaimsCurrentUserContext>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = entraOptions.RequireHttpsMetadata;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    RoleClaimType = entraOptions.Claims.RoleClaimType,
                    NameClaimType = entraOptions.Claims.UserIdClaimTypes.First()
                };
                options.Audience = entraOptions.ClientId;
            });

        AuthorizationBuilder authorization = services.AddAuthorizationBuilder();

        authorization.SetDefaultPolicy(
            new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build());
        ConfigureRolePolicies(authorization);

        return services;
    }

    private static void ConfigureRolePolicies(AuthorizationBuilder authorization)
    {
        foreach (UserRole role in Enum.GetValues<UserRole>())
        {
            authorization.AddPolicy(
                EntraAuthorizationPolicies.ForRole(role),
                policy => policy.RequireRole(role.ToString()));
        }

        authorization.AddPolicy(
            EntraAuthorizationPolicies.ApproveOrReject,
            policy => policy.RequireRole(UserRole.Approver.ToString(), UserRole.System.ToString()));

        authorization.AddPolicy(
            EntraAuthorizationPolicies.ViewSettlement,
            policy => policy.RequireRole(
                UserRole.Spender.ToString(),
                UserRole.Approver.ToString(),
                UserRole.ApAccountant.ToString(),
                UserRole.AppAdmin.ToString()));
    }
}

public static class EntraAuthorizationPolicies
{
    public static string ForRole(UserRole role) => $"role:{role}";
    public const string ApproveOrReject = "settlements:approve-or-reject";
    public const string ViewSettlement = "settlements:view";
}
