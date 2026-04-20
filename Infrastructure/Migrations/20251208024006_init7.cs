using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "public",
                table: "sys_user",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sys_department",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "部门名称"),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "部门代码"),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaderId = table.Column<Guid>(type: "uuid", nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "联系电话"),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "邮箱"),
                    Sort = table.Column<int>(type: "integer", nullable: false),
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
                        name: "FK_sys_department_sys_department_ParentId",
                        column: x => x.ParentId,
                        principalTable: "sys_department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sys_department_sys_user_LeaderId",
                        column: x => x.LeaderId,
                        principalSchema: "public",
                        principalTable: "sys_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
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
                    ExecutionDuration = table.Column<long>(type: "bigint", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
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

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_DepartmentId",
                schema: "public",
                table: "sys_user",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "idx_department_code",
                table: "sys_department",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_department_LeaderId",
                table: "sys_department",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_department_ParentId",
                table: "sys_department",
                column: "ParentId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_sys_user_sys_department_DepartmentId",
                schema: "public",
                table: "sys_user",
                column: "DepartmentId",
                principalTable: "sys_department",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sys_user_sys_department_DepartmentId",
                schema: "public",
                table: "sys_user");

            migrationBuilder.DropTable(
                name: "sys_department");

            migrationBuilder.DropTable(
                name: "sys_operation_log");

            migrationBuilder.DropIndex(
                name: "IX_sys_user_DepartmentId",
                schema: "public",
                table: "sys_user");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "public",
                table: "sys_user");
        }
    }
}
