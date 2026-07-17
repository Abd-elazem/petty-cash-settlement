using Microsoft.Extensions.Options;

namespace PettyCash.Infrastructure.SharePoint.Configuration;

public sealed class SharePointFoundationOptionsValidator : IValidateOptions<SharePointFoundationOptions>
{
    public ValidateOptionsResult Validate(string? name, SharePointFoundationOptions options)
    {
        // Keep validation no-op when foundation is disabled so local/dev defaults don't fail startup.
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var errors = new List<string>();

        if (options.GraphAuth.Mode == GraphAuthMode.ClientSecret)
        {
            if (string.IsNullOrWhiteSpace(options.GraphAuth.TenantId))
            {
                errors.Add("SharePoint:GraphAuth:TenantId is required when GraphAuth:Mode is ClientSecret.");
            }

            if (string.IsNullOrWhiteSpace(options.GraphAuth.ClientId))
            {
                errors.Add("SharePoint:GraphAuth:ClientId is required when GraphAuth:Mode is ClientSecret.");
            }

            if (string.IsNullOrWhiteSpace(options.GraphAuth.ClientSecret))
            {
                errors.Add("SharePoint:GraphAuth:ClientSecret is required when GraphAuth:Mode is ClientSecret.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(options.GraphAuth.ManagedIdentityEndpoint))
            {
                errors.Add("SharePoint:GraphAuth:ManagedIdentityEndpoint is required when GraphAuth:Mode is ManagedIdentity.");
            }

            if (string.IsNullOrWhiteSpace(options.GraphAuth.ManagedIdentityResource))
            {
                errors.Add("SharePoint:GraphAuth:ManagedIdentityResource is required when GraphAuth:Mode is ManagedIdentity.");
            }
        }

        if (string.IsNullOrWhiteSpace(options.GraphAuth.Scope))
        {
            errors.Add("SharePoint:GraphAuth:Scope is required.");
        }

        if (!Uri.TryCreate(options.GraphApi.BaseUrl, UriKind.Absolute, out _))
        {
            errors.Add("SharePoint:GraphApi:BaseUrl must be an absolute URL.");
        }
        if (string.IsNullOrWhiteSpace(options.ReferenceData.SiteId))
        {
            errors.Add("SharePoint:ReferenceData:SiteId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ReferenceData.CategoryMappingsListId))
        {
            errors.Add("SharePoint:ReferenceData:CategoryMappingsListId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ReferenceData.AppUserProfilesListId))
        {
            errors.Add("SharePoint:ReferenceData:AppUserProfilesListId is required.");
        }
        if (string.IsNullOrWhiteSpace(options.Settlements.SiteId))
        {
            errors.Add("SharePoint:Settlements:SiteId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Settlements.SettlementHeadersListId))
        {
            errors.Add("SharePoint:Settlements:SettlementHeadersListId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Settlements.SettlementLinesListId))
        {
            errors.Add("SharePoint:Settlements:SettlementLinesListId is required.");
        }
        if (string.IsNullOrWhiteSpace(options.Audit.SiteId))
        {
            errors.Add("SharePoint:Audit:SiteId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audit.AuditLogsListId))
        {
            errors.Add("SharePoint:Audit:AuditLogsListId is required.");
        }

        if (options.GraphApi.TimeoutSeconds <= 0 || options.GraphApi.TimeoutSeconds > 300)
        {
            errors.Add("SharePoint:GraphApi:TimeoutSeconds must be between 1 and 300.");
        }

        if (options.Retry.MaxAttempts <= 0 || options.Retry.MaxAttempts > 10)
        {
            errors.Add("SharePoint:Retry:MaxAttempts must be between 1 and 10.");
        }

        if (options.Retry.BaseDelayMilliseconds < 0)
        {
            errors.Add("SharePoint:Retry:BaseDelayMilliseconds must be >= 0.");
        }

        if (options.Retry.MaxDelayMilliseconds < options.Retry.BaseDelayMilliseconds)
        {
            errors.Add("SharePoint:Retry:MaxDelayMilliseconds must be >= BaseDelayMilliseconds.");
        }

        if (options.Retry.JitterMilliseconds < 0 || options.Retry.JitterMilliseconds > 2_000)
        {
            errors.Add("SharePoint:Retry:JitterMilliseconds must be between 0 and 2000.");
        }

        if (options.Correlation.AddCorrelationHeader && string.IsNullOrWhiteSpace(options.Correlation.HeaderName))
        {
            errors.Add("SharePoint:Correlation:HeaderName is required when AddCorrelationHeader is true.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
