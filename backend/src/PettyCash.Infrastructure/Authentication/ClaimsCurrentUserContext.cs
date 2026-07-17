using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PettyCash.Application.Abstractions;

namespace PettyCash.Infrastructure.Authentication;

public sealed class ClaimsCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly EntraAuthenticationOptions _options;

    public ClaimsCurrentUserContext(
        IHttpContextAccessor httpContextAccessor,
        IOptions<EntraAuthenticationOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public CurrentUser Current
    {
        get
        {
            ClaimsPrincipal principal = _httpContextAccessor.HttpContext?.User
                ?? new ClaimsPrincipal(new ClaimsIdentity());

            string userId = ResolveFirstClaimValue(principal, _options.Claims.UserIdClaimTypes)
                ?? principal.Identity?.Name
                ?? "unknown";

            string email = ResolveFirstClaimValue(principal, _options.Claims.EmailClaimTypes)
                ?? principal.FindFirstValue(ClaimTypes.Email)
                ?? userId;

            IReadOnlyCollection<UserRole> roles = ResolveRoles(principal);

            return new CurrentUser(userId, email, roles);
        }
    }

    private IReadOnlyCollection<UserRole> ResolveRoles(ClaimsPrincipal principal)
    {
        return principal.Claims
            .Where(claim => string.Equals(claim.Type, _options.Claims.RoleClaimType, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Select(TryParseRole)
            .OfType<UserRole>()
            .Distinct()
            .ToArray();
    }

    private static UserRole? TryParseRole(string roleValue)
    {
        return Enum.TryParse<UserRole>(roleValue, ignoreCase: true, out UserRole role)
            ? role
            : null;
    }

    private static string? ResolveFirstClaimValue(ClaimsPrincipal principal, IReadOnlyCollection<string> claimTypes)
    {
        foreach (string claimType in claimTypes)
        {
            string? value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
