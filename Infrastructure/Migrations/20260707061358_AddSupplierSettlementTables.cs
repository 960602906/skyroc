using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierSettlementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_bill",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    bill_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "供应商待结单据业务唯一编号"),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联供应商主键"),
                    supplier_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的供应商名称快照"),
                    source_type = table.Column<int>(type: "integer", nullable: false, comment: "待结单据来源类型：采购入库或采购退货出库"),
                    stock_in_order_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源采购入库单主键"),
                    stock_out_order_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源采购退货出库单主键"),
                    source_document_no_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "来源出入库单业务编号快照"),
                    bill_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "账单生成或最近一次同步的业务日期（UTC）"),
                    document_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "来源出入库单据绝对金额合计，按系统业务币种计量且始终为非负数"),
                    payable_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "当前待结绝对金额，采购入库为正、采购退货为负，按系统业务币种计量"),
                    settled_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "已结款金额，按系统业务币种计量"),
                    bill_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "供应商待结单据结款状态：待结款、部分结款或已结款"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "供应商待结单据备注，记录人工调整说明或同步异常说明"),
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
                    table.PrimaryKey("PK_supplier_bill", x => x.id);
                    table.CheckConstraint("ck_supplier_bill_amounts", "document_amount >= 0 AND settled_amount >= 0 AND settled_amount <= document_amount AND ((source_type = 1 AND payable_amount >= 0) OR (source_type = 2 AND payable_amount <= 0))");
                    table.CheckConstraint("ck_supplier_bill_source", "(source_type = 1 AND stock_in_order_id IS NOT NULL AND stock_out_order_id IS NULL) OR (source_type = 2 AND stock_out_order_id IS NOT NULL AND stock_in_order_id IS NULL)");
                    table.CheckConstraint("ck_supplier_bill_status", "bill_status BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_supplier_bill_stock_in_order_stock_in_order_id",
                        column: x => x.stock_in_order_id,
                        principalTable: "stock_in_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_bill_stock_out_order_stock_out_order_id",
                        column: x => x.stock_out_order_id,
                        principalTable: "stock_out_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_bill_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "供应商待结单据，按采购入库或采购退货出库汇总应付金额和结款状态");

            migrationBuilder.CreateTable(
                name: "supplier_settlement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    settlement_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "供应商结算单业务唯一编号"),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联供应商主键"),
                    supplier_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的供应商名称快照"),
                    settlement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "客户实际结款日期（UTC）"),
                    serial_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "外部交易流水号"),
                    should_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "结款前待结金额合计，按系统业务币种计量"),
                    payment_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "实际收款金额，按系统业务币种计量"),
                    discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "优惠减免金额，按系统业务币种计量"),
                    applied_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次增加的已结金额，等于收款金额与优惠金额合计"),
                    remaining_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次处理后的剩余待结金额，按系统业务币种计量"),
                    settlement_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "供应商结算单状态：作废、待结款、部分结款或已结款"),
                    voided_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "凭证作废时间（UTC）"),
                    voided_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "作废操作人主键"),
                    voided_by_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "作废操作人名称快照"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "供应商结款或作废备注，记录付款渠道、优惠原因或回滚说明"),
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
                    table.PrimaryKey("PK_supplier_settlement", x => x.id);
                    table.CheckConstraint("ck_supplier_settlement_amounts", "should_amount >= 0 AND payment_amount >= 0 AND discount_amount >= 0 AND applied_amount = payment_amount + discount_amount AND remaining_amount >= 0");
                    table.CheckConstraint("ck_supplier_settlement_status", "settlement_status IN (-1, 1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_supplier_settlement_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "供应商结算单，记录向供应商付款、优惠和对应待结单据余额核销结果");

            migrationBuilder.CreateTable(
                name: "supplier_bill_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    supplier_bill_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属或被核销的供应商待结单据主键"),
                    source_type = table.Column<int>(type: "integer", nullable: false, comment: "明细来源类型：采购入库或采购退货出库"),
                    source_document_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源业务单据主键，用于追溯订单或售后来源"),
                    source_detail_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源业务明细主键，用于幂等识别订单商品或售后商品"),
                    stock_in_order_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源采购入库单主键"),
                    stock_in_detail_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源采购入库商品明细主键"),
                    stock_out_order_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源采购退货出库单主键"),
                    stock_out_detail_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源采购退货出库商品明细主键"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务发生时的商品编码快照"),
                    goods_type_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "业务发生时的商品分类名称快照"),
                    goods_unit_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "供应商待结单据明细使用的商品单位主键"),
                    goods_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "供应商待结单据明细使用的商品单位名称快照"),
                    base_unit_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "商品基础单位主键"),
                    base_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "业务发生时的基础单位名称快照"),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "账单数量，正数表示采购入库、负数表示采购退货，按当前商品单位计量"),
                    base_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "账单基础数量，正数表示采购入库、负数表示采购退货，按基础单位计量"),
                    conversion_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "当前单位换算为基础单位的比例"),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "账单采用的商品单价，按当前商品单位和系统业务币种计量"),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "账单明细应付金额，正数增加应付、负数冲减应付"),
                    business_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "来源业务事实发生时间（UTC）"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "账单明细备注，记录价格差异或退货原因"),
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
                    table.PrimaryKey("PK_supplier_bill_detail", x => x.id);
                    table.CheckConstraint("ck_supplier_bill_detail_conversion_rate", "conversion_rate > 0");
                    table.CheckConstraint("ck_supplier_bill_detail_source_amount", "(source_type = 1 AND quantity >= 0 AND base_quantity >= 0 AND amount >= 0) OR (source_type = 2 AND quantity <= 0 AND base_quantity <= 0 AND amount <= 0)");
                    table.CheckConstraint("ck_supplier_bill_detail_source_type", "source_type IN (1, 2)");
                    table.CheckConstraint("ck_supplier_bill_detail_unit_price", "unit_price >= 0");
                    table.ForeignKey(
                        name: "FK_supplier_bill_detail_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_bill_detail_goods_unit_base_unit_id",
                        column: x => x.base_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_supplier_bill_detail_goods_unit_goods_unit_id",
                        column: x => x.goods_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_bill_detail_stock_in_detail_stock_in_detail_id",
                        column: x => x.stock_in_detail_id,
                        principalTable: "stock_in_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_bill_detail_stock_in_order_stock_in_order_id",
                        column: x => x.stock_in_order_id,
                        principalTable: "stock_in_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_bill_detail_stock_out_detail_stock_out_detail_id",
                        column: x => x.stock_out_detail_id,
                        principalTable: "stock_out_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_bill_detail_stock_out_order_stock_out_order_id",
                        column: x => x.stock_out_order_id,
                        principalTable: "stock_out_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_bill_detail_supplier_bill_supplier_bill_id",
                        column: x => x.supplier_bill_id,
                        principalTable: "supplier_bill",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "供应商待结单据明细，记录入库或退货商品行对供应商应付的影响");

            migrationBuilder.CreateTable(
                name: "supplier_settlement_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    supplier_settlement_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属供应商结算单主键"),
                    supplier_bill_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "被核销的供应商待结单据主键"),
                    supplier_bill_no_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "被核销供应商待结单据编号快照"),
                    source_type = table.Column<int>(type: "integer", nullable: false, comment: "被核销单据来源类型快照：采购入库或采购退货出库"),
                    source_document_no_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "来源出入库单业务编号快照"),
                    stock_in_order_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源采购入库单主键"),
                    stock_out_order_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源采购退货出库单主键"),
                    payable_amount_snapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "结款时单据应付金额快照"),
                    previous_settled_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次结款前账单已结金额快照"),
                    payment_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "实际收款金额，按系统业务币种计量"),
                    discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "优惠减免金额，按系统业务币种计量"),
                    applied_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次增加的已结金额，等于收款金额与优惠金额合计"),
                    current_settled_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次结款后账单已结金额快照"),
                    remaining_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次处理后的剩余待结金额，按系统业务币种计量"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "供应商结款明细备注，记录单张单据的优惠或差异说明"),
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
                    table.PrimaryKey("PK_supplier_settlement_detail", x => x.id);
                    table.CheckConstraint("ck_supplier_settlement_detail_amounts", "payable_amount_snapshot <> 0 AND previous_settled_amount >= 0 AND payment_amount >= 0 AND discount_amount >= 0 AND applied_amount = payment_amount + discount_amount AND current_settled_amount >= previous_settled_amount AND remaining_amount >= 0");
                    table.ForeignKey(
                        name: "FK_supplier_settlement_detail_stock_in_order_stock_in_order_id",
                        column: x => x.stock_in_order_id,
                        principalTable: "stock_in_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_settlement_detail_stock_out_order_stock_out_order_~",
                        column: x => x.stock_out_order_id,
                        principalTable: "stock_out_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_settlement_detail_supplier_bill_supplier_bill_id",
                        column: x => x.supplier_bill_id,
                        principalTable: "supplier_bill",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_settlement_detail_supplier_settlement_supplier_set~",
                        column: x => x.supplier_settlement_id,
                        principalTable: "supplier_settlement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "供应商结算单明细，记录单张待结单据在本次结款中的金额变化");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_bill_no",
                table: "supplier_bill",
                column: "bill_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_supplier_bill_status_date",
                table: "supplier_bill",
                columns: new[] { "bill_status", "bill_date" });

            migrationBuilder.CreateIndex(
                name: "idx_supplier_bill_stock_in_order_id",
                table: "supplier_bill",
                column: "stock_in_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_supplier_bill_stock_out_order_id",
                table: "supplier_bill",
                column: "stock_out_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_supplier_bill_supplier_date",
                table: "supplier_bill",
                columns: new[] { "supplier_id", "bill_date" });

            migrationBuilder.CreateIndex(
                name: "idx_supplier_bill_detail_base_unit_id",
                table: "supplier_bill_detail",
                column: "base_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_bill_detail_bill_id",
                table: "supplier_bill_detail",
                column: "supplier_bill_id");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_bill_detail_goods_id",
                table: "supplier_bill_detail",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_bill_detail_source_detail",
                table: "supplier_bill_detail",
                columns: new[] { "source_type", "source_detail_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_supplier_bill_detail_stock_in_detail_id",
                table: "supplier_bill_detail",
                column: "stock_in_detail_id");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_bill_detail_stock_out_detail_id",
                table: "supplier_bill_detail",
                column: "stock_out_detail_id");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_bill_detail_unit_id",
                table: "supplier_bill_detail",
                column: "goods_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_bill_detail_stock_in_order_id",
                table: "supplier_bill_detail",
                column: "stock_in_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_bill_detail_stock_out_order_id",
                table: "supplier_bill_detail",
                column: "stock_out_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_settlement_no",
                table: "supplier_settlement",
                column: "settlement_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_supplier_settlement_serial_no",
                table: "supplier_settlement",
                column: "serial_no");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_settlement_status_date",
                table: "supplier_settlement",
                columns: new[] { "settlement_status", "settlement_date" });

            migrationBuilder.CreateIndex(
                name: "idx_supplier_settlement_supplier_date",
                table: "supplier_settlement",
                columns: new[] { "supplier_id", "settlement_date" });

            migrationBuilder.CreateIndex(
                name: "idx_supplier_settlement_detail_bill_id",
                table: "supplier_settlement_detail",
                column: "supplier_bill_id");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_settlement_detail_settlement_bill",
                table: "supplier_settlement_detail",
                columns: new[] { "supplier_settlement_id", "supplier_bill_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_supplier_settlement_detail_stock_in_order_id",
                table: "supplier_settlement_detail",
                column: "stock_in_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_supplier_settlement_detail_stock_out_order_id",
                table: "supplier_settlement_detail",
                column: "stock_out_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_bill_detail");

            migrationBuilder.DropTable(
                name: "supplier_settlement_detail");

            migrationBuilder.DropTable(
                name: "supplier_bill");

            migrationBuilder.DropTable(
                name: "supplier_settlement");
        }
    }
}
