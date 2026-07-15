using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PettyCash.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUserProfiles",
                columns: table => new
                {
                    AppUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WorkerId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApproverEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserProfiles", x => x.AppUserId);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PerformedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoryMappings",
                columns: table => new
                {
                    CategoryCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpenseMainAccount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DimensionDefaults = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SalesTaxGroup = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ItemSalesTaxGroup = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    KmRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryMappings", x => x.CategoryCode);
                });

            // NOTE: no "xmin" column is created here, deliberately. EF's migration generator
            // would normally emit migrationBuilder.AddColumn<uint>(name: "xmin", type: "xid", ...)
            // for the shadow concurrency-token property configured in SettlementConfiguration —
            // this is a confirmed Npgsql provider quirk (npgsql/efcore.pg#145, #3270), not
            // something this project introduced. "xmin" is a PostgreSQL SYSTEM column that
            // already exists implicitly on every table; attempting to add it fails at runtime
            // with 42701 ("columnxmin conflicts with a system column name"). The model still
            // describes the shadow property (see the ModelSnapshot/Designer files) for EF's own
            // change-tracking and concurrency-token purposes — it just requires no DDL of its own.
            migrationBuilder.CreateTable(
                name: "Settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SettlementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SpenderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SpenderNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WorkerIdSnapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApproverEmailSnapshot = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ApprovalComment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    JournalBatchNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettlementLines",
                columns: table => new
                {
                    LineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNo = table.Column<int>(type: "integer", nullable: false),
                    CategoryCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpenseMainAccountSnapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DimensionDefaultsSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsVat = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    VatGross = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CarPlate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OdometerKm = table.Column<decimal>(type: "numeric(10,1)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementLines", x => x.LineId);
                    table.ForeignKey(
                        name: "FK_SettlementLines_Settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "Settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SettlementId",
                table: "AuditLogs",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementLines_SettlementId",
                table: "SettlementLines",
                column: "SettlementId");

            // Dev seed data — Sprint 4 deliverable #7. Matches CategoryMappingConfiguration/
            // AppUserProfileConfiguration's HasData calls exactly (column order = record
            // declaration order).
            migrationBuilder.InsertData(
                table: "CategoryMappings",
                columns: new[] { "CategoryCode", "ExpenseMainAccount", "DimensionDefaults", "SalesTaxGroup", "ItemSalesTaxGroup", "KmRequired", "Active" },
                values: new object[,]
                {
                    { "OFFICE_SUPPLIES", "6100", "Dept:Admin", null, null, false, true },
                    { "FUEL", "6200", "Dept:Fleet", "TXFULL", "ITFULL", true, true },
                    { "GOVERNMENT_FEES", "6300", "Dept:Legal", null, null, false, true }
                });

            migrationBuilder.InsertData(
                table: "AppUserProfiles",
                columns: new[] { "AppUserId", "DisplayName", "WorkerId", "ApproverEmail", "Active" },
                values: new object[,]
                {
                    { "spender.demo", "Demo Spender", "W-0001", "manager.demo@canex.com", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditLogs");
            migrationBuilder.DropTable(name: "CategoryMappings");
            migrationBuilder.DropTable(name: "SettlementLines");
            migrationBuilder.DropTable(name: "AppUserProfiles");
            migrationBuilder.DropTable(name: "Settlements");
        }
    }
}
