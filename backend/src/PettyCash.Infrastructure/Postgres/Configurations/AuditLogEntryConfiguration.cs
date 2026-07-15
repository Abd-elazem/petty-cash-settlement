using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PettyCash.Application.Abstractions;

namespace PettyCash.Infrastructure.Postgres.Configurations;

/// <summary>
/// AuditLogEntry (Application record, A-013) has no Id property by design — it's an
/// Application-level shape, not tied to any storage concern. EF's shadow-property support
/// gives it a synthetic Guid primary key here without adding an Id field to the record itself.
/// </summary>
public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogs");
        builder.Property<Guid>("Id").ValueGeneratedOnAdd();
        builder.HasKey("Id");

        builder.Property(a => a.SettlementId).IsRequired();
        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
        builder.Property(a => a.PerformedByUserId).IsRequired().HasMaxLength(100);
        builder.Property(a => a.FromStatus).HasMaxLength(20);
        builder.Property(a => a.ToStatus).HasMaxLength(20);
        builder.Property(a => a.OccurredAtUtc).IsRequired();
        builder.Property(a => a.Details).HasMaxLength(2000);

        builder.HasIndex(a => a.SettlementId);
    }
}
