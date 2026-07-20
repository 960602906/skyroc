using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPrintTemplateIsEnabledBoolSentinel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "is_enabled",
                table: "print_template",
                type: "boolean",
                nullable: false,
                comment: "模板是否允许业务打印选择",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true,
                oldComment: "模板是否允许业务打印选择");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "is_enabled",
                table: "print_template",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "模板是否允许业务打印选择",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "模板是否允许业务打印选择");
        }
    }
}
