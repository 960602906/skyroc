using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sys_menu_button_sys_menu_MenuId",
                schema: "public",
                table: "sys_menu_button");

            migrationBuilder.RenameColumn(
                name: "Desc",
                schema: "public",
                table: "sys_menu_button",
                newName: "desc");

            migrationBuilder.RenameColumn(
                name: "Code",
                schema: "public",
                table: "sys_menu_button",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "MenuId",
                schema: "public",
                table: "sys_menu_button",
                newName: "menu_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sys_menu_button_sys_menu_menu_id",
                schema: "public",
                table: "sys_menu_button",
                column: "menu_id",
                principalSchema: "public",
                principalTable: "sys_menu",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sys_menu_button_sys_menu_menu_id",
                schema: "public",
                table: "sys_menu_button");

            migrationBuilder.RenameColumn(
                name: "desc",
                schema: "public",
                table: "sys_menu_button",
                newName: "Desc");

            migrationBuilder.RenameColumn(
                name: "code",
                schema: "public",
                table: "sys_menu_button",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "menu_id",
                schema: "public",
                table: "sys_menu_button",
                newName: "MenuId");

            migrationBuilder.AddForeignKey(
                name: "FK_sys_menu_button_sys_menu_MenuId",
                schema: "public",
                table: "sys_menu_button",
                column: "MenuId",
                principalSchema: "public",
                principalTable: "sys_menu",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
