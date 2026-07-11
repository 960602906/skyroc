using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrinting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "print_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    template_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "打印模板稳定业务编码"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "供管理员在打印模板列表和选择器中识别的模板名称"),
                    business_type = table.Column<int>(type: "integer", nullable: false, comment: "打印模板适用的业务单据类型：销售订单、采购单、入库单、出库单、客户结款或供应商结算"),
                    design_json = table.Column<string>(type: "text", nullable: false, comment: "前端打印设计器保存的 JSON 配置"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "模板是否允许业务打印选择"),
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
                    table.PrimaryKey("PK_print_template", x => x.id);
                    table.CheckConstraint("ck_print_template_business_type", "business_type BETWEEN 1 AND 6");
                },
                comment: "打印模板，保存业务单据可复用的设计器 JSON 和启用状态");

            migrationBuilder.CreateTable(
                name: "print_template_field",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    print_template_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属打印模板主键"),
                    field_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "打印数据 JSON 中的稳定字段路径"),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "模板设计器中展示的字段业务名称"),
                    display_order = table.Column<int>(type: "integer", nullable: false, comment: "字段在模板设计器面板中的升序位置"),
                    format = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "字段渲染格式提示，由前端打印引擎解释"),
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
                    table.PrimaryKey("PK_print_template_field", x => x.id);
                    table.CheckConstraint("ck_print_template_field_order", "display_order >= 0");
                    table.ForeignKey(
                        name: "FK_print_template_field_print_template_print_template_id",
                        column: x => x.print_template_id,
                        principalTable: "print_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "打印模板字段定义，记录设计器可绑定的数据路径、名称和展示顺序");

            migrationBuilder.CreateIndex(
                name: "idx_print_template_type_enabled",
                table: "print_template",
                columns: new[] { "business_type", "is_enabled" });

            migrationBuilder.CreateIndex(
                name: "uk_print_template_code",
                table: "print_template",
                column: "template_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_print_template_field_key",
                table: "print_template_field",
                columns: new[] { "print_template_id", "field_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_print_template_field_order",
                table: "print_template_field",
                columns: new[] { "print_template_id", "display_order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "print_template_field");

            migrationBuilder.DropTable(
                name: "print_template");
        }
    }
}
