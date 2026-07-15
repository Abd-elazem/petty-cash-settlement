using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PettyCash.Infrastructure.Postgres;

#nullable disable

namespace PettyCash.Infrastructure.Migrations
{
    [DbContext(typeof(PettyCashDbContext))]
    partial class PettyCashDbContextModelSnapshot : ModelSnapshot
    {
        /// <inheritdoc />
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("PettyCash.Application.Abstractions.AppUserProfileReadModel", b =>
            {
                b.Property<string>("AppUserId").HasMaxLength(100).HasColumnType("character varying(100)");
                b.Property<string>("ApproverEmail").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
                b.Property<bool>("Active").HasColumnType("boolean");
                b.Property<string>("DisplayName").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                b.Property<string>("WorkerId").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
                b.HasKey("AppUserId");
                b.ToTable("AppUserProfiles");
                b.HasData(
                    new { AppUserId = "spender.demo", ApproverEmail = "manager.demo@canex.com", Active = true, DisplayName = "Demo Spender", WorkerId = "W-0001" });
            });

            modelBuilder.Entity("PettyCash.Application.Abstractions.AuditLogEntry", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid");
                b.Property<string>("Action").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
                b.Property<string>("Details").HasMaxLength(2000).HasColumnType("character varying(2000)");
                b.Property<string>("FromStatus").HasMaxLength(20).HasColumnType("character varying(20)");
                b.Property<DateTime>("OccurredAtUtc").HasColumnType("timestamp with time zone");
                b.Property<string>("PerformedByUserId").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
                b.Property<Guid>("SettlementId").HasColumnType("uuid");
                b.Property<string>("ToStatus").HasMaxLength(20).HasColumnType("character varying(20)");
                b.HasKey("Id");
                b.HasIndex("SettlementId");
                b.ToTable("AuditLogs");
            });

            modelBuilder.Entity("PettyCash.Application.Abstractions.CategoryMappingReadModel", b =>
            {
                b.Property<string>("CategoryCode").HasMaxLength(50).HasColumnType("character varying(50)");
                b.Property<bool>("Active").HasColumnType("boolean");
                b.Property<string>("DimensionDefaults").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                b.Property<string>("ExpenseMainAccount").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
                b.Property<string>("ItemSalesTaxGroup").HasMaxLength(50).HasColumnType("character varying(50)");
                b.Property<bool>("KmRequired").HasColumnType("boolean");
                b.Property<string>("SalesTaxGroup").HasMaxLength(50).HasColumnType("character varying(50)");
                b.HasKey("CategoryCode");
                b.ToTable("CategoryMappings");
                b.HasData(
                    new { CategoryCode = "OFFICE_SUPPLIES", Active = true, DimensionDefaults = "Dept:Admin", ExpenseMainAccount = "6100", KmRequired = false },
                    new { CategoryCode = "FUEL", Active = true, DimensionDefaults = "Dept:Fleet", ExpenseMainAccount = "6200", ItemSalesTaxGroup = "ITFULL", KmRequired = true, SalesTaxGroup = "TXFULL" },
                    new { CategoryCode = "GOVERNMENT_FEES", Active = true, DimensionDefaults = "Dept:Legal", ExpenseMainAccount = "6300", KmRequired = false });
            });

            modelBuilder.Entity("PettyCash.Domain.Settlements.Settlement", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uuid");
                b.Property<string>("ApprovalComment").HasMaxLength(1000).HasColumnType("character varying(1000)");
                b.Property<string>("ApproverEmailSnapshot").IsRequired().HasMaxLength(256).HasColumnType("character varying(256)");
                b.Property<string>("JournalBatchNumber").HasMaxLength(50).HasColumnType("character varying(50)");
                b.Property<string>("Purpose").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)");
                b.Property<DateOnly>("SettlementDate").HasColumnType("date");
                b.Property<string>("SpenderId").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
                b.Property<string>("SpenderNameSnapshot").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                b.Property<string>("Status").IsRequired().HasMaxLength(20).HasColumnType("character varying(20)");
                b.Property<int>("Version").HasColumnType("integer");
                b.Property<string>("WorkerIdSnapshot").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");

                // See InitialCreate.Up() for why no DDL exists for this shadow property.
                b.Property<uint>("xmin").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasColumnType("xid");

                b.HasKey("Id");
                b.ToTable("Settlements");
            });

            modelBuilder.Entity("PettyCash.Domain.Settlements.Settlement", b =>
            {
                b.OwnsMany("PettyCash.Domain.Settlements.SettlementLine", "Lines", b1 =>
                {
                    b1.Property<Guid>("LineId").ValueGeneratedNever().HasColumnType("uuid");
                    b1.Property<Guid>("SettlementId").HasColumnType("uuid");
                    b1.Property<string>("CategoryCode").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
                    b1.Property<string>("DimensionDefaultsSnapshot").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                    b1.Property<string>("ExpenseMainAccountSnapshot").IsRequired().HasMaxLength(50).HasColumnType("character varying(50)");
                    b1.Property<bool>("IsVat").HasColumnType("boolean");
                    b1.Property<int>("LineNo").HasColumnType("integer");
                    b1.Property<string>("Notes").HasMaxLength(1000).HasColumnType("character varying(1000)");

                    b1.HasKey("LineId");
                    b1.HasIndex("SettlementId");
                    b1.ToTable("SettlementLines");
                    b1.WithOwner().HasForeignKey("SettlementId");

                    b1.OwnsOne("PettyCash.Domain.Settlements.Money", "GrossAmount", b2 =>
                    {
                        b2.Property<Guid>("SettlementLineLineId").HasColumnType("uuid");
                        b2.Property<decimal>("Amount").HasColumnType("numeric(18,2)").HasColumnName("GrossAmount");
                        b2.Property<string>("Currency").IsRequired().HasMaxLength(3).HasColumnType("character varying(3)").HasColumnName("Currency");
                        b2.HasKey("SettlementLineLineId");
                        b2.ToTable("SettlementLines");
                        b2.WithOwner().HasForeignKey("SettlementLineLineId");
                    });

                    b1.OwnsOne("PettyCash.Domain.Settlements.VatBreakdown", "VatBreakdown", b2 =>
                    {
                        b2.Property<Guid>("SettlementLineLineId").HasColumnType("uuid");
                        b2.Property<decimal>("Gross").HasColumnType("numeric(18,2)").HasColumnName("VatGross");
                        b2.Property<decimal>("Vat").HasColumnType("numeric(18,2)").HasColumnName("VatAmount");
                        b2.Property<decimal>("Net").HasColumnType("numeric(18,2)").HasColumnName("NetAmount");
                        b2.HasKey("SettlementLineLineId");
                        b2.ToTable("SettlementLines");
                        b2.WithOwner().HasForeignKey("SettlementLineLineId");
                    });

                    b1.OwnsOne("PettyCash.Domain.Settlements.OdometerReading", "Odometer", b2 =>
                    {
                        b2.Property<Guid>("SettlementLineLineId").HasColumnType("uuid");
                        b2.Property<string>("CarPlate").HasMaxLength(20).HasColumnType("character varying(20)").HasColumnName("CarPlate");
                        b2.Property<decimal>("OdometerKm").HasColumnType("numeric(10,1)").HasColumnName("OdometerKm");
                        b2.HasKey("SettlementLineLineId");
                        b2.ToTable("SettlementLines");
                        b2.WithOwner().HasForeignKey("SettlementLineLineId");
                    });

                    b1.Navigation("GrossAmount").IsRequired();
                    b1.Navigation("VatBreakdown").IsRequired();
                    b1.Navigation("Odometer").IsRequired(false);
                });

                b.Navigation("Lines");
            });
#pragma warning restore 612, 618
        }
    }
}
