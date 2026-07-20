using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixGoodsIsOnSaleBoolSentinel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "is_on_sale",
                table: "quotation_goods",
                type: "boolean",
                nullable: false,
                comment: "商品是否允许上架销售",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true,
                oldComment: "商品是否允许上架销售");

            migrationBuilder.AlterColumn<bool>(
                name: "is_on_sale",
                table: "goods",
                type: "boolean",
                nullable: false,
                comment: "商品是否允许上架销售",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true,
                oldComment: "商品是否允许上架销售");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "is_on_sale",
                table: "quotation_goods",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "商品是否允许上架销售",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "商品是否允许上架销售");

            migrationBuilder.AlterColumn<bool>(
                name: "is_on_sale",
                table: "goods",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "商品是否允许上架销售",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "商品是否允许上架销售");
        }
    }
}
