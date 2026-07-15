using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PettyCash.Application.Abstractions;

namespace PettyCash.Infrastructure.Postgres.Configurations;

public sealed class AppUserProfileConfiguration : IEntityTypeConfiguration<AppUserProfileReadModel>
{
    public void Configure(EntityTypeBuilder<AppUserProfileReadModel> builder)
    {
        builder.ToTable("AppUserProfiles");
        builder.HasKey(u => u.AppUserId);
        builder.Property(u => u.AppUserId).HasMaxLength(100);
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.WorkerId).HasMaxLength(50);
        builder.Property(u => u.ApproverEmail).IsRequired().HasMaxLength(256);
        builder.Property(u => u.Active).IsRequired();

        // Dev seed data — one active spender so CreateDraftSettlementCommand is exercisable
        // end-to-end without an admin UI (Guide §5.2's admin-maintained profile table, not yet built).
        builder.HasData(
            new AppUserProfileReadModel("spender.demo", "Demo Spender", "W-0001", "manager.demo@canex.com", Active: true));
    }
}
