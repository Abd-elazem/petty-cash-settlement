using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PettyCash.Domain.Settlements;

namespace PettyCash.Infrastructure.Postgres.Configurations;

/// <summary>
/// Maps the Settlement aggregate. Settlement.Lines is configured as OwnsMany rather than
/// a normal HasMany/DbSet relationship: SettlementLine has its own identity (LineId) but
/// is never independently queried or persisted outside its owning Settlement (Domain's
/// own rule — see SettlementLine's class doc), which is exactly what EF's "owned entity"
/// concept models. There is no DbSet&lt;SettlementLine&gt; anywhere, by design.
/// </summary>
public sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("Settlements");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever(); // Domain generates the Guid itself (Settlement.CreateDraft)

        // Optimistic concurrency via Postgres's native xmin system column — zero-cost,
        // needs no application-level RowVersion column, and needs no Domain change.
        // UseXminAsConcurrencyToken() was obsoleted in Npgsql.EntityFrameworkCore.PostgreSQL
        // 7.0+ and removed in later versions (confirmed against 9.0.4, the version this
        // project pins) — the officially documented replacement is a shadow `uint` property
        // named "xmin", configured via standard EF Core mechanisms so it maps onto Postgres's
        // actual xmin system column by name. Not a custom solution: this is Npgsql's own
        // documented pattern (npgsql.org/efcore/modeling/concurrency.html), not a workaround.
        // (ARCHITECTURE.md's original schema sketch proposed an explicit RowVersion field;
        // this is the concrete implementation of that same intent — see DECISIONS.md D-023.)
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.Property(s => s.Version)
            .HasColumnName("Version")
            .IsRequired(); // business version counter (reject→edit→resubmit cycle, A-002/A-003) — distinct from the xmin concurrency token above

        builder.Property(s => s.SettlementDate).IsRequired();
        builder.Property(s => s.Purpose).IsRequired().HasMaxLength(500);
        builder.Property(s => s.SpenderId).IsRequired().HasMaxLength(100);
        builder.Property(s => s.SpenderNameSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(s => s.WorkerIdSnapshot).HasMaxLength(50);
        builder.Property(s => s.ApproverEmailSnapshot).IsRequired().HasMaxLength(256);
        builder.Property(s => s.ApprovalComment).HasMaxLength(1000);
        builder.Property(s => s.JournalBatchNumber).HasMaxLength(50);

        // Stored as text, not int, so the database is human-readable when someone inspects
        // it directly during development — this is a dev-only adapter, favor debuggability.
        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Computed, not persisted — recomputed from Lines every time, never trusted from storage.
        builder.Ignore(s => s.TotalAmount);
        builder.Ignore(s => s.IsEditable);

        var lines = builder.OwnsMany(s => s.Lines, l => ConfigureLines(l));

        // Settlement.Lines has no public setter (only a getter returning _lines.AsReadOnly()),
        // so EF must write through the private backing field. EF's naming convention would
        // find "_lines" automatically, but we set it explicitly rather than rely on convention
        // guessing correctly for a financial system's core aggregate.
        builder.Metadata.FindNavigation(nameof(Settlement.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureLines(OwnedNavigationBuilder<Settlement, SettlementLine> lines)
    {
        lines.ToTable("SettlementLines");
        lines.WithOwner().HasForeignKey("SettlementId");
        lines.HasKey(l => l.LineId);
        lines.Property(l => l.LineId).ValueGeneratedNever();

        lines.Property(l => l.LineNo).IsRequired();
        lines.Property(l => l.CategoryCode).IsRequired().HasMaxLength(50);
        lines.Property(l => l.ExpenseMainAccountSnapshot).IsRequired().HasMaxLength(50);
        lines.Property(l => l.DimensionDefaultsSnapshot).IsRequired(false).HasMaxLength(200);
        lines.Property(l => l.IsVat).IsRequired();
        lines.Property(l => l.Notes).HasMaxLength(1000);

        // Money/VatBreakdown/OdometerReading are all materialized via EF's constructor-parameter
        // binding (their constructor parameter names match their property names), so none of
        // them need a parameterless constructor or public setters — unlike Settlement/SettlementLine
        // themselves, which are real entities, not value objects.
        lines.OwnsOne(l => l.GrossAmount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("GrossAmount").HasColumnType("numeric(18,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        lines.OwnsOne(l => l.VatBreakdown, vat =>
        {
            vat.Property(v => v.Gross).HasColumnName("VatGross").HasColumnType("numeric(18,2)").IsRequired();
            vat.Property(v => v.Vat).HasColumnName("VatAmount").HasColumnType("numeric(18,2)").IsRequired();
            vat.Property(v => v.Net).HasColumnName("NetAmount").HasColumnType("numeric(18,2)").IsRequired();
        });

        // Optional owned type: EF stores all columns NULL when the navigation itself is null
        // (non-fuel lines), and materializes null back rather than an instance. The nullable
        // reference annotation (OdometerReading?) drives this via convention already, but for
        // a financial system this distinction (fuel vs. non-fuel line) is significant enough
        // to state explicitly rather than lean on an implicit convention holding.
        lines.OwnsOne(l => l.Odometer, odo =>
        {
            odo.Property(o => o.CarPlate).HasColumnName("CarPlate").HasMaxLength(20);
            odo.Property(o => o.OdometerKm).HasColumnName("OdometerKm").HasColumnType("numeric(10,1)");
        });
        lines.Navigation(l => l.Odometer).IsRequired(false);
    }
}
