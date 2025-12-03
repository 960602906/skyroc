using System;
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
            migrationBuilder.CreateTable(
                name: "sys_menu_button",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "按钮编码"),
                    Desc = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "按钮描述"),
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属菜单ID"),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_menu_button", x => x.id);
                    table.ForeignKey(
                        name: "FK_sys_menu_button_sys_menu_MenuId",
                        column: x => x.MenuId,
                        principalSchema: "public",
                        principalTable: "sys_menu",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_menu_buttons_menu_id_code",
                schema: "public",
                table: "sys_menu_button",
                columns: new[] { "MenuId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_menu_buttons_menuId",
                schema: "public",
                table: "sys_menu_button",
                column: "MenuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sys_menu_button",
                schema: "public");
        }
    }
}
