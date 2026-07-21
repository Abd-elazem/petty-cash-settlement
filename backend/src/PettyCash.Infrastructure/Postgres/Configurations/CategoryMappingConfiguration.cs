using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PettyCash.Application.Abstractions;

namespace PettyCash.Infrastructure.Postgres.Configurations;

/// <summary>Finance-owned reference data (Guide §5.5). Read-only from the Application's perspective — no Add/Update methods exist on ICategoryMappingRepository, so this table is seeded/maintained out-of-band (admin tooling, a future milestone) rather than through this app's write path.</summary>
public sealed class CategoryMappingConfiguration : IEntityTypeConfiguration<CategoryMappingReadModel>
{
    public void Configure(EntityTypeBuilder<CategoryMappingReadModel> builder)
    {
        builder.ToTable("CategoryMappings");
        builder.HasKey(c => c.CategoryCode);
        builder.Property(c => c.CategoryCode).HasMaxLength(50);
        builder.Property(c => c.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ExpenseMainAccount).IsRequired().HasMaxLength(50);
        builder.Property(c => c.DimensionDefaults).HasMaxLength(200);
        builder.Property(c => c.SalesTaxGroup).HasMaxLength(50);
        builder.Property(c => c.ItemSalesTaxGroup).HasMaxLength(50);
        builder.Property(c => c.KmRequired).IsRequired();
        builder.Property(c => c.Active).IsRequired();

        // Dev seed data (deliverable #7) — enough to exercise every AddLineCommand path
        // (a non-fuel category and a fuel/KmRequired category) without a real Finance-owned
        // mapping list existing yet.
        builder.HasData(
            new CategoryMappingReadModel("OFFICE_SUPPLIES", "Office Supplies", "6100", "Dept:Admin", null, null, KmRequired: false, Active: true),
            new CategoryMappingReadModel("FUEL", "Fuel", "6200", "Dept:Fleet", "TXFULL", "ITFULL", KmRequired: true, Active: true),
            new CategoryMappingReadModel("GOVERNMENT_FEES", "Government Fees", "6300", "Dept:Legal", null, null, KmRequired: false, Active: true));
    }
}
