using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTraceabilityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_push_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    business_type = table.Column<int>(type: "integer", nullable: false, comment: "报送来源业务类型：销售订单、检测报告或溯源记录"),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "报送来源业务主键，按业务类型指向订单、报告或溯源记录"),
                    business_no_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "报送发起时的来源业务编号快照"),
                    platform_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "目标外部平台的稳定标识编码"),
                    push_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "外部报送状态：待报送、报送成功或报送失败"),
                    push_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "报送发起时间（UTC）"),
                    response_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "外部平台返回响应的时间（UTC）"),
                    request_content = table.Column<string>(type: "text", nullable: true, comment: "报送请求报文的脱敏序列化内容"),
                    response_content = table.Column<string>(type: "text", nullable: true, comment: "外部平台响应报文的脱敏序列化内容"),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "报送失败时记录的错误摘要；成功时为空"),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "当前业务在本次记录前已重试报送的次数"),
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
                    table.PrimaryKey("PK_external_push_log", x => x.id);
                    table.CheckConstraint("ck_external_push_log_business_type", "business_type BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_external_push_log_retry", "retry_count >= 0");
                    table.CheckConstraint("ck_external_push_log_status", "push_status BETWEEN 1 AND 3");
                },
                comment: "外部报送日志，只追加记录向外部监管或溯源平台每次报送的请求、响应和结果状态");

            migrationBuilder.CreateTable(
                name: "inspection_report",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    inspection_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "检测报告业务唯一编号"),
                    stock_in_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源采购入库单主键，报告商品明细必须来自该入库单"),
                    in_no_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "建报时的来源采购入库单编号快照"),
                    ware_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "接收入库商品的仓库主键"),
                    ware_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "建报时的仓库名称快照"),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "供货供应商主键；来源入库单没有供应商时为空"),
                    supplier_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "建报时的供应商名称快照"),
                    inspection_org = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "出具检测结论的检测机构名称"),
                    sample_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "商品抽样时间（UTC）"),
                    inspect_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "检测完成时间（UTC）"),
                    conclusion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "质量检测结论：待定、合格或不合格"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "报告级业务备注，对全部报告商品生效"),
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
                    table.PrimaryKey("PK_inspection_report", x => x.id);
                    table.CheckConstraint("ck_inspection_report_conclusion", "conclusion BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_inspection_report_stock_in_order_stock_in_order_id",
                        column: x => x.stock_in_order_id,
                        principalTable: "stock_in_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspection_report_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_inspection_report_ware_ware_id",
                        column: x => x.ware_id,
                        principalTable: "ware",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "检测报告，按采购入库单记录商品质量检测机构、时间和整单结论快照");

            migrationBuilder.CreateTable(
                name: "inspection_attachment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    inspection_report_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联检测报告主键"),
                    attachment_type = table.Column<int>(type: "integer", nullable: false, comment: "附件类型：报告文件或现场图片"),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "附件原始文件名，含扩展名"),
                    file_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "附件的可访问存储地址"),
                    file_size = table.Column<long>(type: "bigint", nullable: true, comment: "附件文件大小，单位为字节"),
                    sort = table.Column<int>(type: "integer", nullable: false, comment: "同一报告内附件的展示顺序，值越小越靠前"),
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
                    table.PrimaryKey("PK_inspection_attachment", x => x.id);
                    table.CheckConstraint("ck_inspection_attachment_file_size", "file_size >= 0");
                    table.CheckConstraint("ck_inspection_attachment_type", "attachment_type IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_inspection_attachment_inspection_report_inspection_report_id",
                        column: x => x.inspection_report_id,
                        principalTable: "inspection_report",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "检测报告附件，记录报告文件或现场图片的访问地址和展示顺序");

            migrationBuilder.CreateTable(
                name: "inspection_report_goods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    inspection_report_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联检测报告主键"),
                    stock_in_detail_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源采购入库商品明细主键"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "送检商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "建报时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "建报时的商品编码快照"),
                    goods_type_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "建报时的商品分类名称快照；商品未挂分类时为空"),
                    goods_unit_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "送检数量使用的计量单位主键，与来源入库明细的入库单位一致"),
                    goods_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "建报时的送检计量单位名称快照"),
                    sample_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "送检商品数量，按送检单位计量且必须大于零"),
                    batch_no_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "建报时的入库批次号快照，用于追溯到具体库存批次"),
                    conclusion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "质量检测结论：待定、合格或不合格"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "仅针对当前报告商品行的检测说明或不合格原因"),
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
                    table.PrimaryKey("PK_inspection_report_goods", x => x.id);
                    table.CheckConstraint("ck_inspection_report_goods_conclusion", "conclusion BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_inspection_report_goods_quantity", "sample_quantity > 0");
                    table.ForeignKey(
                        name: "FK_inspection_report_goods_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspection_report_goods_goods_unit_goods_unit_id",
                        column: x => x.goods_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspection_report_goods_inspection_report_inspection_report~",
                        column: x => x.inspection_report_id,
                        principalTable: "inspection_report",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inspection_report_goods_stock_in_detail_stock_in_detail_id",
                        column: x => x.stock_in_detail_id,
                        principalTable: "stock_in_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "检测报告商品明细，按入库商品行记录商品、单位、批次快照和单品检测结论");

            migrationBuilder.CreateTable(
                name: "trace_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    trace_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "溯源记录业务唯一编号，作为二维码对外访问标识"),
                    sale_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源销售订单主键"),
                    sale_order_no_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "生成溯源时的销售订单编号快照"),
                    sale_order_detail_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源销售订单商品明细主键"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "下单客户主键"),
                    customer_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "生成溯源时的客户名称快照"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "溯源商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "生成溯源时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "生成溯源时的商品编码快照"),
                    goods_type_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "生成溯源时的商品分类名称快照；商品未挂分类时为空"),
                    stock_in_detail_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "采购来源入库商品明细主键；尚未匹配到入库来源时为空"),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "采购来源供应商主键；入库来源缺失或没有供应商时为空"),
                    supplier_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "生成溯源时的供应商名称快照"),
                    ware_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "采购来源入库仓库主键；入库来源缺失时为空"),
                    ware_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "生成溯源时的入库仓库名称快照"),
                    batch_no_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "生成溯源时的入库批次号快照"),
                    inspection_report_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联检测报告主键；来源入库商品尚未出具报告时为空"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "溯源记录备注，记录来源匹配差异或人工补录说明"),
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
                    table.PrimaryKey("PK_trace_record", x => x.id);
                    table.ForeignKey(
                        name: "FK_trace_record_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trace_record_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trace_record_inspection_report_inspection_report_id",
                        column: x => x.inspection_report_id,
                        principalTable: "inspection_report",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trace_record_sale_order_detail_sale_order_detail_id",
                        column: x => x.sale_order_detail_id,
                        principalTable: "sale_order_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trace_record_sale_order_sale_order_id",
                        column: x => x.sale_order_id,
                        principalTable: "sale_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trace_record_stock_in_detail_stock_in_detail_id",
                        column: x => x.stock_in_detail_id,
                        principalTable: "stock_in_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trace_record_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_trace_record_ware_ware_id",
                        column: x => x.ware_id,
                        principalTable: "ware",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "商品溯源记录，将销售订单商品行与采购入库来源和检测报告串联供二维码展示");

            migrationBuilder.CreateIndex(
                name: "idx_external_push_log_business",
                table: "external_push_log",
                columns: new[] { "business_type", "business_id" });

            migrationBuilder.CreateIndex(
                name: "idx_external_push_log_platform_time",
                table: "external_push_log",
                columns: new[] { "platform_code", "push_time" });

            migrationBuilder.CreateIndex(
                name: "idx_external_push_log_status_time",
                table: "external_push_log",
                columns: new[] { "push_status", "push_time" });

            migrationBuilder.CreateIndex(
                name: "idx_inspection_attachment_report_sort",
                table: "inspection_attachment",
                columns: new[] { "inspection_report_id", "sort" });

            migrationBuilder.CreateIndex(
                name: "idx_inspection_report_conclusion_time",
                table: "inspection_report",
                columns: new[] { "conclusion", "inspect_time" });

            migrationBuilder.CreateIndex(
                name: "idx_inspection_report_no",
                table: "inspection_report",
                column: "inspection_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inspection_report_stock_in_order_id",
                table: "inspection_report",
                column: "stock_in_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_inspection_report_supplier_id",
                table: "inspection_report",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_inspection_report_ware_id",
                table: "inspection_report",
                column: "ware_id");

            migrationBuilder.CreateIndex(
                name: "idx_inspection_report_goods_goods_id",
                table: "inspection_report_goods",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_inspection_report_goods_source",
                table: "inspection_report_goods",
                columns: new[] { "inspection_report_id", "stock_in_detail_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inspection_report_goods_stock_in_detail_id",
                table: "inspection_report_goods",
                column: "stock_in_detail_id");

            migrationBuilder.CreateIndex(
                name: "idx_inspection_report_goods_unit_id",
                table: "inspection_report_goods",
                column: "goods_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_trace_record_customer_id",
                table: "trace_record",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_trace_record_goods_id",
                table: "trace_record",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_trace_record_inspection_report_id",
                table: "trace_record",
                column: "inspection_report_id");

            migrationBuilder.CreateIndex(
                name: "idx_trace_record_no",
                table: "trace_record",
                column: "trace_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_trace_record_sale_order_detail_id",
                table: "trace_record",
                column: "sale_order_detail_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_trace_record_sale_order_id",
                table: "trace_record",
                column: "sale_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_trace_record_stock_in_detail_id",
                table: "trace_record",
                column: "stock_in_detail_id");

            migrationBuilder.CreateIndex(
                name: "idx_trace_record_supplier_id",
                table: "trace_record",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_trace_record_ware_id",
                table: "trace_record",
                column: "ware_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_push_log");

            migrationBuilder.DropTable(
                name: "inspection_attachment");

            migrationBuilder.DropTable(
                name: "inspection_report_goods");

            migrationBuilder.DropTable(
                name: "trace_record");

            migrationBuilder.DropTable(
                name: "inspection_report");
        }
    }
}
