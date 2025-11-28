using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "sys_menu",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "varchar(50)", nullable: false),
                    path = table.Column<string>(type: "varchar(100)", nullable: false),
                    component = table.Column<string>(type: "varchar(100)", nullable: true),
                    title = table.Column<string>(type: "varchar(100)", nullable: false),
                    i18nKey = table.Column<string>(type: "varchar(100)", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: true),
                    keep_alive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    constant = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    icon = table.Column<string>(type: "varchar(100)", nullable: true),
                    local_icon = table.Column<string>(type: "varchar(100)", nullable: true),
                    href = table.Column<string>(type: "varchar(200)", nullable: true),
                    hide_in_menu = table.Column<bool>(type: "boolean", nullable: false),
                    active_menu = table.Column<string>(type: "varchar(200)", nullable: true),
                    multi_tab = table.Column<bool>(type: "boolean", nullable: true),
                    fixed_index_in_tab = table.Column<int>(type: "integer", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_menu", x => x.id);
                    table.ForeignKey(
                        name: "FK_sys_menu_sys_menu_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "public",
                        principalTable: "sys_menu",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sys_role",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "varchar(50)", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", nullable: false),
                    desc = table.Column<string>(type: "varchar(200)", nullable: true),
                    created_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_user",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    username = table.Column<string>(type: "varchar(50)", nullable: false),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    nick_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    phone = table.Column<string>(type: "varchar(20)", nullable: true),
                    email = table.Column<string>(type: "varchar(100)", nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", nullable: false),
                    created_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_role_menu",
                schema: "public",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_role_menu", x => new { x.role_id, x.menu_id });
                    table.ForeignKey(
                        name: "FK_sys_role_menu_sys_menu_menu_id",
                        column: x => x.menu_id,
                        principalSchema: "public",
                        principalTable: "sys_menu",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_role_menu_sys_role_role_id",
                        column: x => x.role_id,
                        principalSchema: "public",
                        principalTable: "sys_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_refresh_token",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdateBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_refresh_token", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_refresh_token_sys_user_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "sys_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sys_user_role",
                schema: "public",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user_role", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_sys_user_role_sys_role_role_id",
                        column: x => x.role_id,
                        principalSchema: "public",
                        principalTable: "sys_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sys_user_role_sys_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "sys_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_menu_parent_id",
                schema: "public",
                table: "sys_menu",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_sys_refresh_token_Token",
                schema: "public",
                table: "sys_refresh_token",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_refresh_token_UserId",
                schema: "public",
                table: "sys_refresh_token",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_role_name",
                schema: "public",
                table: "sys_role",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_role_menu_menu_id",
                schema: "public",
                table: "sys_role_menu",
                column: "menu_id");

            migrationBuilder.CreateIndex(
                name: "idx_role_menu_role_id",
                schema: "public",
                table: "sys_role_menu",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_email",
                schema: "public",
                table: "sys_user",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_username",
                schema: "public",
                table: "sys_user",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_role_role_id",
                schema: "public",
                table: "sys_user_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_role_user_id",
                schema: "public",
                table: "sys_user_role",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sys_refresh_token",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sys_role_menu",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sys_user_role",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sys_menu",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sys_role",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sys_user",
                schema: "public");
        }
    }
}
