using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStoredFileTableCommentForObjectStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "stored_file",
                comment: "受保护上传文件元数据，记录经签名校验后写入对象存储的存储键、类型、大小和创建人",
                oldComment: "受保护上传文件元数据，记录经签名校验后的文件存储键、类型、大小和创建人");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "stored_file",
                comment: "受保护上传文件元数据，记录经签名校验后的文件存储键、类型、大小和创建人",
                oldComment: "受保护上传文件元数据，记录经签名校验后写入对象存储的存储键、类型、大小和创建人");
        }
    }
}
