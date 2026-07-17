namespace PettyCash.Infrastructure.Authentication;

public sealed class EntraAuthenticationOptions
{
    public const string SectionName = "Authentication:Entra";

    public bool Enabled { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string? Authority { get; init; }
    public bool RequireHttpsMetadata { get; init; } = true;
    public EntraClaimsMappingOptions Claims { get; init; } = new();
}

public sealed class EntraClaimsMappingOptions
{
    public string RoleClaimType { get; init; } = "roles";
    public string[] UserIdClaimTypes { get; init; } =
    [
        "oid",
        "sub",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
    ];
    public string[] EmailClaimTypes { get; init; } =
    [
        "preferred_username",
        "upn",
        "email",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
    ];
}
