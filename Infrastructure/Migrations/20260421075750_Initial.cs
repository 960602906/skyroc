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
                    menu_type = table.Column<int>(type: "integer", nullable: true),
                    layout = table.Column<string>(type: "varchar(100)", nullable: true),
                    redirect = table.Column<string>(type: "varchar(100)", nullable: true),
                    component = table.Column<string>(type: "varchar(100)", nullable: true),
                    title = table.Column<string>(type: "varchar(100)", nullable: false),
                    i18nKey = table.Column<string>(type: "varchar(100)", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: true),
                    keep_alive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    constant = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    icon = table.Column<string>(type: "varchar(100)", nullable: true),
                    local_icon = table.Column<string>(type: "varchar(100)", nullable: true),
                    icon_type = table.Column<int>(type: "integer", nullable: true),
                    href = table.Column<string>(type: "varchar(200)", nullable: true),
                    hide_in_menu = table.Column<bool>(type: "boolean", nullable: false),
                    active_menu = table.Column<string>(type: "varchar(200)", nullable: true),
                    multi_tab = table.Column<bool>(type: "boolean", nullable: true),
                    fixed_index_in_tab = table.Column<int>(type: "integer", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                name: "sys_operation_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    module = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    operation_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    desc = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    request_params = table.Column<string>(type: "text", nullable: true),
                    response_result = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    browser = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    os = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    execution_duration = table.Column<long>(type: "bigint", nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_sys_operation_log", x => x.id);
                },
                comment: "操作日志表");

            migrationBuilder.CreateTable(
                name: "sys_role",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "varchar(50)", nullable: false),
                    code = table.Column<string>(type: "varchar(50)", nullable: false),
                    desc = table.Column<string>(type: "varchar(200)", nullable: true),
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
                    table.PrimaryKey("PK_sys_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sys_menu_button",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "按钮编码"),
                    desc = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "按钮描述"),
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属菜单ID"),
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
                        name: "FK_sys_menu_button_sys_menu_menu_id",
                        column: x => x.menu_id,
                        principalSchema: "public",
                        principalTable: "sys_menu",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "sys_department",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "部门名称"),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "部门代码"),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    leader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    leader_name = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "联系电话"),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "邮箱"),
                    sort = table.Column<int>(type: "integer", nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "备注"),
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
                    table.PrimaryKey("PK_sys_department", x => x.id);
                    table.ForeignKey(
                        name: "FK_sys_department_sys_department_parent_id",
                        column: x => x.parent_id,
                        principalTable: "sys_department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "sys_user",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    username = table.Column<string>(type: "varchar(50)", nullable: false),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    nick_name = table.Column<string>(type: "varchar(50)", nullable: false),
                    phone = table.Column<string>(type: "varchar(20)", nullable: true),
                    email = table.Column<string>(type: "varchar(100)", nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", nullable: false),
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
                    table.PrimaryKey("PK_sys_user", x => x.id);
                    table.ForeignKey(
                        name: "FK_sys_user_sys_department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "sys_department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
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
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreateName = table.Column<string>(type: "text", nullable: true),
                    UpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdateBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdateName = table.Column<string>(type: "text", nullable: true),
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
                name: "idx_department_code",
                table: "sys_department",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_department_leader_id",
                table: "sys_department",
                column: "leader_id");

            migrationBuilder.CreateIndex(
                name: "IX_sys_department_parent_id",
                table: "sys_department",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "idx_menu_parent_id",
                schema: "public",
                table: "sys_menu",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "idx_menu_buttons_menu_id_code",
                schema: "public",
                table: "sys_menu_button",
                columns: new[] { "menu_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_menu_buttons_menuId",
                schema: "public",
                table: "sys_menu_button",
                column: "menu_id");

            migrationBuilder.CreateIndex(
                name: "idx_operation_log_create_by",
                table: "sys_operation_log",
                column: "create_by");

            migrationBuilder.CreateIndex(
                name: "idx_operation_log_create_time",
                table: "sys_operation_log",
                column: "create_time");

            migrationBuilder.CreateIndex(
                name: "idx_operation_log_module_type",
                table: "sys_operation_log",
                columns: new[] { "module", "operation_type" });

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
                name: "idx_role_code",
                schema: "public",
                table: "sys_role",
                column: "code",
                unique: true);

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
                name: "IX_sys_user_DepartmentId",
                schema: "public",
                table: "sys_user",
                column: "DepartmentId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_sys_department_sys_user_leader_id",
                table: "sys_department",
                column: "leader_id",
                principalSchema: "public",
                principalTable: "sys_user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sys_department_sys_user_leader_id",
                table: "sys_department");

            migrationBuilder.DropTable(
                name: "sys_menu_button",
                schema: "public");

            migrationBuilder.DropTable(
                name: "sys_operation_log");

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

            migrationBuilder.DropTable(
                name: "sys_department");
        }
    }
}
