using Microsoft.Extensions.Options;

namespace PettyCash.Infrastructure.Authentication;

public sealed class EntraAuthenticationOptionsValidator : IValidateOptions<EntraAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, EntraAuthenticationOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.TenantId))
        {
            errors.Add("Authentication:Entra:TenantId is required when Entra authentication is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            errors.Add("Authentication:Entra:ClientId is required when Entra authentication is enabled.");
        }

        if (!string.IsNullOrWhiteSpace(options.Authority) &&
            !Uri.TryCreate(options.Authority, UriKind.Absolute, out _))
        {
            errors.Add("Authentication:Entra:Authority must be an absolute URL when provided.");
        }

        if (string.IsNullOrWhiteSpace(options.Claims.RoleClaimType))
        {
            errors.Add("Authentication:Entra:Claims:RoleClaimType is required.");
        }

        if (options.Claims.UserIdClaimTypes.Length == 0 ||
            options.Claims.UserIdClaimTypes.All(string.IsNullOrWhiteSpace))
        {
            errors.Add("Authentication:Entra:Claims:UserIdClaimTypes must contain at least one claim type.");
        }

        if (options.Claims.EmailClaimTypes.Length == 0 ||
            options.Claims.EmailClaimTypes.All(string.IsNullOrWhiteSpace))
        {
            errors.Add("Authentication:Entra:Claims:EmailClaimTypes must contain at least one claim type.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
