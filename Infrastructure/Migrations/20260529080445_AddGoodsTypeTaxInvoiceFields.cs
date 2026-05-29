using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsTypeTaxInvoiceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "default_tax_rate",
                table: "goods_type",
                type: "numeric(8,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invoice_goods_short_name",
                table: "goods_type",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_tax_exempt",
                table: "goods_type",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "tax_category_code",
                table: "goods_type",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_category_name",
                table: "goods_type",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_policy_basis",
                table: "goods_type",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_goods_type_tax_category_code",
                table: "goods_type",
                column: "tax_category_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_goods_type_tax_category_code",
                table: "goods_type");

            migrationBuilder.DropColumn(
                name: "default_tax_rate",
                table: "goods_type");

            migrationBuilder.DropColumn(
                name: "invoice_goods_short_name",
                table: "goods_type");

            migrationBuilder.DropColumn(
                name: "is_tax_exempt",
                table: "goods_type");

            migrationBuilder.DropColumn(
                name: "tax_category_code",
                table: "goods_type");

            migrationBuilder.DropColumn(
                name: "tax_category_name",
                table: "goods_type");

            migrationBuilder.DropColumn(
                name: "tax_policy_basis",
                table: "goods_type");
        }
    }
}
