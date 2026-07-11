using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stored_file",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    storage_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "服务端随机生成的相对存储键，不包含用户输入路径"),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "上传时保留的原始文件名，仅用于下载展示"),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "由文件签名验证后的 MIME 类型"),
                    file_size = table.Column<long>(type: "bigint", nullable: false, comment: "文件实际大小，单位为字节"),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "记录创建时间（UTC）"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: false, comment: "创建记录的用户主键"),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "创建记录的用户名称"),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "记录最后修改时间（UTC）"),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "最后修改记录的用户主键"),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "最后修改记录的用户名称"),
                    status = table.Column<int>(type: "integer", nullable: false, comment: "记录启用状态")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stored_file", x => x.id);
                    table.CheckConstraint("ck_stored_file_size", "file_size > 0 AND file_size <= 10485760");
                },
                comment: "受保护上传文件元数据，记录经签名校验后的文件存储键、类型、大小和创建人");

            migrationBuilder.CreateIndex(
                name: "idx_stored_file_creator_time",
                table: "stored_file",
                columns: new[] { "create_by", "create_time" });

            migrationBuilder.CreateIndex(
                name: "uk_stored_file_storage_key",
                table: "stored_file",
                column: "storage_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stored_file");
        }
    }
}
