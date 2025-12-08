using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sys_department_sys_department_ParentId",
                table: "sys_department");

            migrationBuilder.DropForeignKey(
                name: "FK_sys_department_sys_user_LeaderId",
                table: "sys_department");

            migrationBuilder.RenameColumn(
                name: "IsSuccess",
                table: "sys_operation_log",
                newName: "is_success");

            migrationBuilder.RenameColumn(
                name: "ExecutionDuration",
                table: "sys_operation_log",
                newName: "execution_duration");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "sys_operation_log",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "Sort",
                table: "sys_department",
                newName: "sort");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "sys_department",
                newName: "parent_id");

            migrationBuilder.RenameColumn(
                name: "LeaderId",
                table: "sys_department",
                newName: "leader_id");

            migrationBuilder.RenameIndex(
                name: "IX_sys_department_ParentId",
                table: "sys_department",
                newName: "IX_sys_department_parent_id");

            migrationBuilder.RenameIndex(
                name: "IX_sys_department_LeaderId",
                table: "sys_department",
                newName: "IX_sys_department_leader_id");

            migrationBuilder.AddForeignKey(
                name: "FK_sys_department_sys_department_parent_id",
                table: "sys_department",
                column: "parent_id",
                principalTable: "sys_department",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

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
                name: "FK_sys_department_sys_department_parent_id",
                table: "sys_department");

            migrationBuilder.DropForeignKey(
                name: "FK_sys_department_sys_user_leader_id",
                table: "sys_department");

            migrationBuilder.RenameColumn(
                name: "is_success",
                table: "sys_operation_log",
                newName: "IsSuccess");

            migrationBuilder.RenameColumn(
                name: "execution_duration",
                table: "sys_operation_log",
                newName: "ExecutionDuration");

            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "sys_operation_log",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "sort",
                table: "sys_department",
                newName: "Sort");

            migrationBuilder.RenameColumn(
                name: "parent_id",
                table: "sys_department",
                newName: "ParentId");

            migrationBuilder.RenameColumn(
                name: "leader_id",
                table: "sys_department",
                newName: "LeaderId");

            migrationBuilder.RenameIndex(
                name: "IX_sys_department_parent_id",
                table: "sys_department",
                newName: "IX_sys_department_ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_sys_department_leader_id",
                table: "sys_department",
                newName: "IX_sys_department_LeaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_sys_department_sys_department_ParentId",
                table: "sys_department",
                column: "ParentId",
                principalTable: "sys_department",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_sys_department_sys_user_LeaderId",
                table: "sys_department",
                column: "LeaderId",
                principalSchema: "public",
                principalTable: "sys_user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
