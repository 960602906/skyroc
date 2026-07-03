using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "required_quantity",
                table: "purchase_plan_order_rel",
                type: "numeric(18,6)",
                nullable: false,
                comment: "需求数量，按采购单位计量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "来源订单需求数量，按采购单位计量");

            migrationBuilder.AlterColumn<decimal>(
                name: "required_quantity",
                table: "purchase_plan_detail",
                type: "numeric(18,6)",
                nullable: false,
                comment: "需求数量，按采购单位计量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "来源订单需求数量，按采购单位计量");

            migrationBuilder.AlterColumn<string>(
                name: "purchase_unit_name_snapshot",
                table: "purchase_plan_detail",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "采购业务发生时的采购单位名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "计划生成时的采购单位名称快照");

            migrationBuilder.AlterColumn<string>(
                name: "supplier_name_snapshot",
                table: "purchase_plan",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                comment: "采购业务发生时的供应商名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true,
                oldComment: "计划生成时的供应商名称快照");

            migrationBuilder.AlterColumn<string>(
                name: "purchaser_name_snapshot",
                table: "purchase_plan",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                comment: "采购业务发生时的采购员名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true,
                oldComment: "计划生成时的采购员名称快照");

            migrationBuilder.CreateTable(
                name: "purchase_order",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    purchase_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "采购单业务唯一编号"),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联供应商主键"),
                    supplier_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "采购业务发生时的供应商名称快照"),
                    purchaser_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "负责采购的采购员主键"),
                    purchaser_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "采购业务发生时的采购员名称快照"),
                    purchase_pattern = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "采购模式：供应商直供或市场自采"),
                    receive_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "采购单预计到货时间（UTC）"),
                    business_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "采购单执行状态：草稿、已完成或已取消"),
                    supplier_contact_name_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "采购发生时的供应商联系人姓名快照"),
                    supplier_contact_phone_snapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "采购发生时的供应商联系人电话快照"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "业务备注"),
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
                    table.PrimaryKey("PK_purchase_order", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_order_purchaser_purchaser_id",
                        column: x => x.purchaser_id,
                        principalTable: "purchaser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchase_order_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "采购单，记录供货方、采购责任人、预计到货和执行状态");

            migrationBuilder.CreateTable(
                name: "purchase_order_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "采购单主键"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务发生时的商品编码快照"),
                    goods_info_snapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "采购发生时序列化保存的商品详情快照"),
                    purchase_unit_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "采购计量单位主键"),
                    purchase_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "采购业务发生时的采购单位名称快照"),
                    required_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "需求数量，按采购单位计量"),
                    purchase_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "采购数量，按采购单位计量"),
                    purchase_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "采购单价，按系统业务币种计量"),
                    purchase_total_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "采购数量与采购单价计算后的金额快照"),
                    product_date = table.Column<DateOnly>(type: "date", nullable: true, comment: "采购商品生产日期，仅记录自然日"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "业务备注"),
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
                    table.PrimaryKey("PK_purchase_order_detail", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_order_detail_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_detail_goods_unit_purchase_unit_id",
                        column: x => x.purchase_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_detail_purchase_order_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "采购单商品明细，记录商品、单位、数量、价格和生产日期快照");

            migrationBuilder.CreateTable(
                name: "purchase_order_plan_rel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    purchase_order_detail_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "采购单商品明细主键"),
                    purchase_plan_detail_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "采购计划商品明细主键"),
                    allocated_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "采购单从来源计划占用的数量，按采购单位计量"),
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
                    table.PrimaryKey("PK_purchase_order_plan_rel", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_order_plan_rel_purchase_order_detail_purchase_orde~",
                        column: x => x.purchase_order_detail_id,
                        principalTable: "purchase_order_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_order_plan_rel_purchase_plan_detail_purchase_plan_~",
                        column: x => x.purchase_plan_detail_id,
                        principalTable: "purchase_plan_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "采购单明细与来源采购计划明细的数量关联");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_order_purchase_no",
                table: "purchase_order",
                column: "purchase_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_purchase_order_purchaser_id",
                table: "purchase_order",
                column: "purchaser_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_order_receive_status",
                table: "purchase_order",
                columns: new[] { "receive_time", "business_status" });

            migrationBuilder.CreateIndex(
                name: "idx_purchase_order_supplier_id",
                table: "purchase_order",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_order_detail_goods_id",
                table: "purchase_order_detail",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_order_detail_order_id",
                table: "purchase_order_detail",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_order_detail_unit_id",
                table: "purchase_order_detail",
                column: "purchase_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_order_plan_rel_detail_plan",
                table: "purchase_order_plan_rel",
                columns: new[] { "purchase_order_detail_id", "purchase_plan_detail_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_purchase_order_plan_rel_plan_detail_id",
                table: "purchase_order_plan_rel",
                column: "purchase_plan_detail_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_order_plan_rel");

            migrationBuilder.DropTable(
                name: "purchase_order_detail");

            migrationBuilder.DropTable(
                name: "purchase_order");

            migrationBuilder.AlterColumn<decimal>(
                name: "required_quantity",
                table: "purchase_plan_order_rel",
                type: "numeric(18,6)",
                nullable: false,
                comment: "来源订单需求数量，按采购单位计量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "需求数量，按采购单位计量");

            migrationBuilder.AlterColumn<decimal>(
                name: "required_quantity",
                table: "purchase_plan_detail",
                type: "numeric(18,6)",
                nullable: false,
                comment: "来源订单需求数量，按采购单位计量",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "需求数量，按采购单位计量");

            migrationBuilder.AlterColumn<string>(
                name: "purchase_unit_name_snapshot",
                table: "purchase_plan_detail",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "计划生成时的采购单位名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "采购业务发生时的采购单位名称快照");

            migrationBuilder.AlterColumn<string>(
                name: "supplier_name_snapshot",
                table: "purchase_plan",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                comment: "计划生成时的供应商名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true,
                oldComment: "采购业务发生时的供应商名称快照");

            migrationBuilder.AlterColumn<string>(
                name: "purchaser_name_snapshot",
                table: "purchase_plan",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                comment: "计划生成时的采购员名称快照",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true,
                oldComment: "采购业务发生时的采购员名称快照");
        }
    }
}
