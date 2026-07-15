using PettyCash.Application.Authorization;

namespace PettyCash.Application.Tests.Fakes;

/// <summary>
/// Shared authorization policy instance for handler tests. This wraps the REAL
/// SettlementAuthorizationPolicy, not an always-allow stub — most handler tests
/// (e.g. "wrong owner throws Forbidden") are asserting actual authorization
/// behavior, so a stub that always passes would silently defeat those tests.
/// SettlementAuthorizationPolicy has no dependencies of its own (no repository,
/// no I/O), so reusing one instance across every test is safe and avoids the
/// "cannot instantiate an interface" mistake of writing `new()` at each call site.
/// </summary>
public static class TestAuthorizationPolicy
{
    public static ISettlementAuthorizationPolicy Instance { get; } = new SettlementAuthorizationPolicy();
}
