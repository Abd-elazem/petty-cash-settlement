using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PettyCash.Infrastructure.Migrations
{
    public partial class AddCategoryDisplayNameAndNullableDimensions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "CategoryMappings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE \"CategoryMappings\" SET \"DisplayName\" = CASE \"CategoryCode\" WHEN 'OFFICE_SUPPLIES' THEN 'Office Supplies' WHEN 'FUEL' THEN 'Fuel' WHEN 'GOVERNMENT_FEES' THEN 'Government Fees' ELSE \"CategoryCode\" END;");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "CategoryMappings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: false,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "DimensionDefaults",
                table: "CategoryMappings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "DimensionDefaultsSnapshot",
                table: "SettlementLines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DisplayName", table: "CategoryMappings");

            migrationBuilder.AlterColumn<string>(
                name: "DimensionDefaults",
                table: "CategoryMappings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DimensionDefaultsSnapshot",
                table: "SettlementLines",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}