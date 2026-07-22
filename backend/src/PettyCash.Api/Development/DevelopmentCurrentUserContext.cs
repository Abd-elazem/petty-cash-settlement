using Microsoft.AspNetCore.Http;
using PettyCash.Application.Abstractions;

namespace PettyCash.Api.Development;

/// <summary>
/// Development-only stand-in for <see cref="ICurrentUserContext"/>. Real authentication
/// (JWT interim or Entra External ID, per TECH_STACK.md's Identity entry, A-014) does not
/// exist yet — this exists solely so the DI container has *something* registered for
/// ICurrentUserContext and `dotnet run` / ASP.NET Core's startup DI validation succeeds
/// (the gap this class closes was deliberate and tracked, see InfrastructureServiceCollectionExtensions'
/// own comment and D-025).
///
/// Reads the X-Dev-Role and X-Dev-Username headers to allow local testing of different
/// roles (e.g., Approver for manager inbox) without changing code. Falls back to
/// "spender.demo" and "Spender" if headers are absent, preserving backward compatibility.
///
/// Replaceable: registered behind the same <see cref="ICurrentUserContext"/> interface
/// Application already depends on, so swapping this out later (JWT claims reader, then
/// Entra claims reader) is a one-line DI registration change in Program.cs — no
/// Application or Domain code will need to change.
/// </summary>
public sealed class DevelopmentCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DevelopmentCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser Current
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            var roleHeader = context?.Request.Headers["X-Dev-Role"].ToString();
            var usernameHeader = context?.Request.Headers["X-Dev-Username"].ToString();

            var role = Enum.TryParse<UserRole>(roleHeader, true, out var parsedRole)
                ? parsedRole
                : UserRole.Spender;

            var username = string.IsNullOrWhiteSpace(usernameHeader)
                ? "spender.demo"
                : usernameHeader;

            return new CurrentUser(
                UserId: username,
                Email: $"{username}@canex.com",
                Roles: new[] { role });
        }
    }
}
