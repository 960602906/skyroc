using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportExportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM goods GROUP BY name HAVING COUNT(*) > 1) THEN
                        RAISE EXCEPTION '无法创建商品名称唯一索引：goods 表存在重复名称，请先合并或更正重复商品。';
                    END IF;
                END $$;
                """);

            migrationBuilder.CreateTable(
                name: "import_export_job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    job_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "导入导出任务业务唯一编号"),
                    job_type = table.Column<int>(type: "integer", nullable: false, comment: "导入导出的业务对象类型：当前为商品"),
                    direction = table.Column<int>(type: "integer", nullable: false, comment: "任务方向：导入或导出"),
                    job_status = table.Column<int>(type: "integer", nullable: false, comment: "任务状态：处理中、成功或失败"),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "导入源文件或导出结果文件名"),
                    total_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "CSV 中处理的数据行总数，不含表头"),
                    success_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "成功处理的数据行数"),
                    failure_rows = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "失败处理的数据行数"),
                    error_summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true, comment: "失败行号和原因摘要"),
                    started_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "任务开始读取或生成文件的时间（UTC）"),
                    finished_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "任务完成或失败的时间（UTC）"),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "记录创建时间（UTC）"),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "创建记录的用户主键"),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "创建记录的用户名称"),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "记录最后修改时间（UTC）"),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "最后修改记录的用户主键"),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true, comment: "最后修改记录的用户名称"),
                    status = table.Column<int>(type: "integer", nullable: false, comment: "记录启用状态")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_export_job", x => x.id);
                    table.CheckConstraint("ck_import_export_job_direction", "direction BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_import_export_job_rows", "total_rows >= 0 AND success_rows >= 0 AND failure_rows >= 0 AND success_rows + failure_rows <= total_rows");
                    table.CheckConstraint("ck_import_export_job_status", "job_status BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_import_export_job_type", "job_type BETWEEN 1 AND 1");
                },
                comment: "导入导出任务，记录 CSV 模板、导入或导出的执行状态和结果摘要");

            migrationBuilder.CreateIndex(
                name: "idx_goods_name",
                table: "goods",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_import_export_job_creator_time",
                table: "import_export_job",
                columns: new[] { "create_by", "create_time" });

            migrationBuilder.CreateIndex(
                name: "idx_import_export_job_no",
                table: "import_export_job",
                column: "job_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_import_export_job_state",
                table: "import_export_job",
                columns: new[] { "job_type", "direction", "job_status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_export_job");

            migrationBuilder.DropIndex(
                name: "idx_goods_name",
                table: "goods");
        }
    }
}
