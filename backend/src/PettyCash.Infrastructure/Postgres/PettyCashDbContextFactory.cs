using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PettyCash.Infrastructure.Postgres;

/// <summary>
/// Lets `dotnet ef migrations add`/`dotnet ef database update` construct a DbContext
/// without running the full app's DI composition (which doesn't exist yet — no Api
/// project until a later sprint). Connection string comes from an environment variable
/// so no credential lives in source control; falls back to a conventional local default.
/// </summary>
public sealed class PettyCashDbContextFactory : IDesignTimeDbContextFactory<PettyCashDbContext>
{
    public PettyCashDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PETTYCASH_DEV_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=pettycash_dev;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<PettyCashDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PettyCashDbContext(optionsBuilder.Options);
    }
}
