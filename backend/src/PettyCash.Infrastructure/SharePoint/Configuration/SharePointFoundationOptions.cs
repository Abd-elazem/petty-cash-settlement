namespace PettyCash.Infrastructure.SharePoint.Configuration;

public sealed class SharePointFoundationOptions
{
    public const string SectionName = "SharePoint";

    public bool Enabled { get; init; }
    public SharePointGraphAuthOptions GraphAuth { get; init; } = new();
    public SharePointGraphApiOptions GraphApi { get; init; } = new();
    public SharePointReferenceDataOptions ReferenceData { get; init; } = new();
    public SharePointSettlementsOptions Settlements { get; init; } = new();
    public SharePointAuditOptions Audit { get; init; } = new();
    public SharePointRetryOptions Retry { get; init; } = new();
    public SharePointCorrelationOptions Correlation { get; init; } = new();
}

public enum GraphAuthMode
{
    ClientSecret = 0,
    ManagedIdentity = 1
}

public sealed class SharePointGraphAuthOptions
{
    public GraphAuthMode Mode { get; init; } = GraphAuthMode.ClientSecret;
    public string TenantId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string? ClientSecret { get; init; }
    public string Scope { get; init; } = "https://graph.microsoft.com/.default";

    // Managed identity token endpoint defaults to Azure IMDS endpoint.
    public string ManagedIdentityEndpoint { get; init; } = "http://169.254.169.254/metadata/identity/oauth2/token";
    public string ManagedIdentityApiVersion { get; init; } = "2018-02-01";
    public string ManagedIdentityResource { get; init; } = "https://graph.microsoft.com/";
    public string? ManagedIdentityClientId { get; init; }
}

public sealed class SharePointGraphApiOptions
{
    public string BaseUrl { get; init; } = "https://graph.microsoft.com/v1.0/";
    public int TimeoutSeconds { get; init; } = 100;
}

public sealed class SharePointReferenceDataOptions
{
    public string SiteId { get; init; } = string.Empty;
    public string CategoryMappingsListId { get; init; } = string.Empty;
    public string AppUserProfilesListId { get; init; } = string.Empty;
}

public sealed class SharePointSettlementsOptions
{
    public string SiteId { get; init; } = string.Empty;
    public string SettlementHeadersListId { get; init; } = string.Empty;
    public string SettlementLinesListId { get; init; } = string.Empty;
}

public sealed class SharePointAuditOptions
{
    public string SiteId { get; init; } = string.Empty;
    public string AuditLogsListId { get; init; } = string.Empty;
}

public sealed class SharePointRetryOptions
{
    public int MaxAttempts { get; init; } = 4;
    public int BaseDelayMilliseconds { get; init; } = 200;
    public int MaxDelayMilliseconds { get; init; } = 5_000;
    public int JitterMilliseconds { get; init; } = 100;
}

public sealed class SharePointCorrelationOptions
{
    public bool AddCorrelationHeader { get; init; } = true;
    public string HeaderName { get; init; } = "x-correlation-id";
}
