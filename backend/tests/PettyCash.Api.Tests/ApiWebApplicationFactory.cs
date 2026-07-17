using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PettyCash.Application.Abstractions;
using PettyCash.Infrastructure.Postgres;
using Testcontainers.PostgreSql;
using Xunit;

namespace PettyCash.Api.Tests;

/// <summary>
/// Boots the real, unmodified Api host (Program.cs) against a disposable Testcontainers
/// Postgres instance, then applies the real InitialCreate migration — the same "real
/// database, no mocks" approach TECH_STACK.md locks in for PettyCash.Infrastructure.Tests,
/// applied here at the endpoint/integration level. Requires Docker, same as
/// PettyCash.Infrastructure.Tests (ASSUMPTIONS.md A-017).
///
/// Environment is pinned explicitly to "Development" rather than relying on
/// WebApplicationFactory's own default — Program.cs only registers
/// DevelopmentCurrentUserContext (D-035) and MapOpenApi() inside an IsDevelopment() check,
/// and this codebase should not depend on an ambient default that lives outside it.
///
/// Identity override: for tests that exercise a non-Spender role (Approve/Reject require
/// Approver or System; VS8/VS9/VS10), call CreateClientWithIdentity(user) instead of
/// CreateClient(). This replaces the DI registration for ICurrentUserContext for that
/// client's request scope with a fixed stub returning the given CurrentUser.
/// Program.cs and DevelopmentCurrentUserContext are completely untouched — only the
/// test-side service collection is modified, and only when a caller explicitly opts in.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("pettycash_api_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    // Well-known test identities used across VS8–10 test files. Defined here so every
    // test file references the same values rather than duplicating magic strings.
    internal static readonly CurrentUser ApproverUser = new(
        UserId: "approver.test",
        Email: "manager.demo@canex.com",   // must match ApproverEmailSnapshot seeded in AppUserProfileConfiguration:
                                            // HasData(new AppUserProfileReadModel("spender.demo", ..., "manager.demo@canex.com", ...))
                                            // EnsureCanApproveOrReject does OrdinalIgnoreCase match on this field.
        Roles: new[] { UserRole.Approver });

    internal static readonly CurrentUser SpenderUser = new(
        UserId: "spender.demo",
        Email: "spender.demo@canex.local",
        Roles: new[] { UserRole.Spender });

    /// <summary>
    /// Returns an HttpClient whose requests will be handled with the given CurrentUser
    /// as the resolved ICurrentUserContext.Current, overriding whatever DevelopmentCurrentUserContext
    /// would have returned. Uses WebApplicationFactory.WithWebHostBuilder to layer an
    /// additional ConfigureServices call on top of the existing DI registrations — the
    /// last Scoped registration wins for ICurrentUserContext.
    /// </summary>
    public HttpClient CreateClientWithIdentity(CurrentUser user)
    {
        return WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Re-register ICurrentUserContext as a fixed stub for this client.
                // AddScoped here appends; ASP.NET Core DI resolves the last matching
                // registration when multiple exist for the same service type (Scoped).
                services.AddScoped<ICurrentUserContext>(_ => new FixedCurrentUserContext(user));
            });
        }).CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Overrides appsettings.Development.json's ConnectionStrings:PettyCashDev with
            // the disposable container's connection string — same key Program.cs and
            // AddInfrastructure() already read, so no other configuration wiring changes.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PettyCashDev"] = _container.GetConnectionString(),
            });
        });
    }

    /// <summary>Minimal ICurrentUserContext that always returns the same CurrentUser.</summary>
    private sealed class FixedCurrentUserContext : ICurrentUserContext
    {
        public FixedCurrentUserContext(CurrentUser user) => Current = user;
        public CurrentUser Current { get; }
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Migrate through the real Api service provider so this test exercises the same
        // InitialCreate migration (and its seed data — AppUserProfileConfiguration's
        // "spender.demo" row, which DevelopmentCurrentUserContext must match, D-038) that
        // dev/production use. No EnsureCreated() shortcut.
        using IServiceScope scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PettyCashDbContext>();
        await db.Database.MigrateAsync();
    }

    // Hides WebApplicationFactory's own ValueTask-returning DisposeAsync() so xUnit's
    // IAsyncLifetime.DisposeAsync() (Task-returning) is satisfied — this must also dispose
    // the base host, which xUnit will not otherwise call for an IAsyncLifetime fixture.
    public new async Task DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}
