using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "layout",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "menu_type",
                schema: "public",
                table: "sys_menu",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "redirect",
                schema: "public",
                table: "sys_menu",
                type: "varchar(100)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "layout",
                schema: "public",
                table: "sys_menu");

            migrationBuilder.DropColumn(
                name: "menu_type",
                schema: "public",
                table: "sys_menu");

            migrationBuilder.DropColumn(
                name: "redirect",
                schema: "public",
                table: "sys_menu");
        }
    }
}
