using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerBillTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_bill",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    bill_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "客户账单业务唯一编号"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联客户主键"),
                    customer_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的客户名称快照"),
                    sale_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源销售订单主键"),
                    sale_order_no_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "客户账单来源销售订单编号快照"),
                    bill_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "账单生成或最近一次同步的业务日期（UTC）"),
                    order_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "订单签收形成的正向应收金额，按系统业务币种计量"),
                    after_sale_adjustment_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "售后完成形成的应收调整金额，负数表示冲减客户应收"),
                    receivable_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "当前账单应收金额，按系统业务币种计量"),
                    settled_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "已结款金额，按系统业务币种计量"),
                    bill_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "客户账单结款状态：待结款、部分结款或已结款"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "客户账单备注，记录人工调整说明或同步异常说明"),
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
                    table.PrimaryKey("PK_customer_bill", x => x.id);
                    table.CheckConstraint("ck_customer_bill_amounts", "order_amount >= 0 AND after_sale_adjustment_amount <= 0 AND receivable_amount >= 0 AND settled_amount >= 0 AND settled_amount <= receivable_amount");
                    table.CheckConstraint("ck_customer_bill_status", "bill_status BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_customer_bill_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_bill_sale_order_sale_order_id",
                        column: x => x.sale_order_id,
                        principalTable: "sale_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "客户账单，按销售订单汇总签收应收、售后调整和结款状态");

            migrationBuilder.CreateTable(
                name: "customer_bill_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    customer_bill_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属客户账单主键"),
                    source_type = table.Column<int>(type: "integer", nullable: false, comment: "账单明细来源类型：订单验收或售后调整"),
                    source_document_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源业务单据主键，用于追溯订单或售后来源"),
                    source_detail_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源业务明细主键，用于幂等识别订单商品或售后商品"),
                    sale_order_detail_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源销售订单商品明细主键"),
                    after_sale_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "所属售后单主键"),
                    after_sale_goods_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "需要回收商品的售后商品明细主键"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务发生时的商品编码快照"),
                    goods_type_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "业务发生时的商品分类名称快照"),
                    goods_unit_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "客户账单明细使用的商品单位主键"),
                    goods_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "客户账单明细使用的商品单位名称快照"),
                    base_unit_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "商品基础单位主键"),
                    base_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "业务发生时的基础单位名称快照"),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "账单数量，正数表示确认销售、负数表示售后冲减，按当前商品单位计量"),
                    base_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "账单基础数量，正数表示确认销售、负数表示售后冲减，按基础单位计量"),
                    conversion_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "当前单位换算为基础单位的比例"),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "账单采用的商品单价，按当前商品单位和系统业务币种计量"),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "账单明细金额，正数增加应收、负数冲减应收"),
                    business_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "来源业务事实发生时间（UTC）"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "账单明细备注，记录验收差异、售后原因或调整说明"),
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
                    table.PrimaryKey("PK_customer_bill_detail", x => x.id);
                    table.CheckConstraint("ck_customer_bill_detail_conversion_rate", "conversion_rate > 0");
                    table.CheckConstraint("ck_customer_bill_detail_source_amount", "(source_type = 1 AND quantity >= 0 AND base_quantity >= 0 AND amount >= 0) OR (source_type = 2 AND quantity <= 0 AND base_quantity <= 0 AND amount <= 0)");
                    table.CheckConstraint("ck_customer_bill_detail_source_type", "source_type IN (1, 2)");
                    table.CheckConstraint("ck_customer_bill_detail_unit_price", "unit_price >= 0");
                    table.ForeignKey(
                        name: "FK_customer_bill_detail_after_sale_after_sale_id",
                        column: x => x.after_sale_id,
                        principalTable: "after_sale",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_bill_detail_after_sale_goods_after_sale_goods_id",
                        column: x => x.after_sale_goods_id,
                        principalTable: "after_sale_goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_bill_detail_customer_bill_customer_bill_id",
                        column: x => x.customer_bill_id,
                        principalTable: "customer_bill",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customer_bill_detail_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_bill_detail_goods_unit_base_unit_id",
                        column: x => x.base_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_customer_bill_detail_goods_unit_goods_unit_id",
                        column: x => x.goods_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_bill_detail_sale_order_detail_sale_order_detail_id",
                        column: x => x.sale_order_detail_id,
                        principalTable: "sale_order_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "客户账单明细，记录订单验收和售后调整对客户应收的影响");

            migrationBuilder.CreateIndex(
                name: "idx_customer_bill_customer_date",
                table: "customer_bill",
                columns: new[] { "customer_id", "bill_date" });

            migrationBuilder.CreateIndex(
                name: "idx_customer_bill_no",
                table: "customer_bill",
                column: "bill_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_bill_sale_order_id",
                table: "customer_bill",
                column: "sale_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_bill_status_date",
                table: "customer_bill",
                columns: new[] { "bill_status", "bill_date" });

            migrationBuilder.CreateIndex(
                name: "idx_customer_bill_detail_after_sale_goods_id",
                table: "customer_bill_detail",
                column: "after_sale_goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_bill_detail_after_sale_id",
                table: "customer_bill_detail",
                column: "after_sale_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_bill_detail_base_unit_id",
                table: "customer_bill_detail",
                column: "base_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_bill_detail_bill_id",
                table: "customer_bill_detail",
                column: "customer_bill_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_bill_detail_goods_id",
                table: "customer_bill_detail",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_bill_detail_sale_order_detail_id",
                table: "customer_bill_detail",
                column: "sale_order_detail_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_bill_detail_source_detail",
                table: "customer_bill_detail",
                columns: new[] { "source_type", "source_detail_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_bill_detail_unit_id",
                table: "customer_bill_detail",
                column: "goods_unit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_bill_detail");

            migrationBuilder.DropTable(
                name: "customer_bill");
        }
    }
}
