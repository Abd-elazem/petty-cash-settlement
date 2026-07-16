using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("pettycash_api_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

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
