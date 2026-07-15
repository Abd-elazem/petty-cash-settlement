using Microsoft.EntityFrameworkCore;
using PettyCash.Infrastructure.Postgres;
using Testcontainers.PostgreSql;
using Xunit;

namespace PettyCash.Infrastructure.Tests;

/// <summary>
/// Spins up one disposable Postgres container (postgres:16-alpine) shared across every
/// test class in the "Postgres" collection — one container per test run, not per class,
/// to keep the suite fast. Applies EF migrations on startup (deliverable: migration test),
/// which requires migration files to actually exist in PettyCash.Infrastructure/Migrations.
///
/// KNOWN GAP, flagged rather than worked around: this session could not run
/// `dotnet ef migrations add InitialCreate` (no command-execution access to the user's
/// machine). Until that command is run once, MigrateAsync() below will find zero
/// migrations and every test in this project will fail with a clear message pointing at
/// this comment and the required command — not a mysterious Postgres error.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("pettycash_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public PettyCashDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PettyCashDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return new PettyCashDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = CreateContext();

        var migrations = db.Database.GetMigrations().ToList(); // synchronous-only in the official API — reads the migrations assembly, no DB round-trip, so EF Core never added an async variant
        if (migrations.Count == 0)
        {
            throw new InvalidOperationException(
                "No EF Core migrations found in PettyCash.Infrastructure. Run this once, " +
                "from backend/, before running these tests: " +
                "dotnet ef migrations add InitialCreate " +
                "--project src/PettyCash.Infrastructure --startup-project src/PettyCash.Infrastructure");
        }

        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
}
