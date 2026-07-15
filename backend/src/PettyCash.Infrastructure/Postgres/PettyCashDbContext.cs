using Microsoft.EntityFrameworkCore;
using PettyCash.Application.Abstractions;
using PettyCash.Domain.Settlements;

namespace PettyCash.Infrastructure.Postgres;

/// <summary>
/// The only place EF Core exists in this solution. Nothing outside PettyCash.Infrastructure
/// ever sees this type — Application and Domain have no project reference to Infrastructure
/// at all, so that's structurally guaranteed, not just a convention. Repository
/// implementations in this project are the only consumers.
///
/// DbSets are internal on purpose: even other classes within Infrastructure that aren't
/// repositories (there are none today, but the intent is the constraint) shouldn't reach
/// into the context directly.
/// </summary>
public sealed class PettyCashDbContext : DbContext
{
    public PettyCashDbContext(DbContextOptions<PettyCashDbContext> options) : base(options)
    {
    }

    internal DbSet<Settlement> Settlements => Set<Settlement>();
    internal DbSet<CategoryMappingReadModel> CategoryMappings => Set<CategoryMappingReadModel>();
    internal DbSet<AppUserProfileReadModel> AppUserProfiles => Set<AppUserProfileReadModel>();
    internal DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PettyCashDbContext).Assembly);
    }
}
