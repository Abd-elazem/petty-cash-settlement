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
/// Returns one fixed, hard-coded <see cref="CurrentUser"/> for every request. No claims,
/// no headers, no per-request variation — there is no authentication pipeline yet to
/// derive one from. This is intentionally minimal: it exists to make the container
/// resolvable, not to simulate real identity or role behavior.
///
/// Replaceable: registered behind the same <see cref="ICurrentUserContext"/> interface
/// Application already depends on, so swapping this out later (JWT claims reader, then
/// Entra claims reader) is a one-line DI registration change in Program.cs — no
/// Application or Domain code will need to change.
/// </summary>
public sealed class DevelopmentCurrentUserContext : ICurrentUserContext
{
    // UserId must match the seeded AppUserProfile row (AppUserProfileConfiguration.cs:
    // "spender.demo") — CreateDraftSettlementCommandHandler resolves the profile by this
    // exact id and throws NotFoundException otherwise. Originally "dev-local-user", which
    // does not exist in seed data and made Create Draft Settlement 404 on every local
    // call; found and fixed during Vertical Slice 1 self-review (Api-layer dev-fixture
    // bug only, no Domain/Application change — see CHANGELOG/DECISIONS D-038).
    public CurrentUser Current { get; } = new(
        UserId: "spender.demo",
        Email: "spender.demo@canex.local",
        Roles: new[] { UserRole.Spender });
}
