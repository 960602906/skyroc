using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockCoreTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_batch",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    ware_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联仓库主键"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务发生时的商品编码快照"),
                    batch_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "仓库商品批次号"),
                    base_unit_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "商品基础单位主键"),
                    base_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "业务发生时的基础单位名称快照"),
                    current_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "批次当前账面数量，按商品基础单位计量"),
                    available_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "扣除占用后的可出库数量，按商品基础单位计量"),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "库存单位成本，按系统业务币种和基础单位计量"),
                    product_date = table.Column<DateOnly>(type: "date", nullable: true, comment: "库存批次商品生产日期，仅记录自然日"),
                    expire_date = table.Column<DateOnly>(type: "date", nullable: true, comment: "商品到期日期，仅记录自然日"),
                    last_movement_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "批次最近一次库存变更时间（UTC）"),
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
                    table.PrimaryKey("PK_stock_batch", x => x.id);
                    table.CheckConstraint("ck_stock_batch_available_quantity", "available_quantity >= 0 AND available_quantity <= current_quantity");
                    table.CheckConstraint("ck_stock_batch_current_quantity", "current_quantity >= 0");
                    table.CheckConstraint("ck_stock_batch_unit_cost", "unit_cost >= 0");
                    table.ForeignKey(
                        name: "FK_stock_batch_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_batch_goods_unit_base_unit_id",
                        column: x => x.base_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_batch_ware_ware_id",
                        column: x => x.ware_id,
                        principalTable: "ware",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "库存批次，记录仓库商品批次的当前数量、可用数量和成本");

            migrationBuilder.CreateTable(
                name: "stock_in_order",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    in_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "入库单业务唯一编号"),
                    order_type = table.Column<int>(type: "integer", nullable: false, comment: "库存单据业务类型"),
                    business_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "入库单状态：草稿、待审核、已审核、已反审核或已删除"),
                    ware_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联仓库主键"),
                    ware_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "库存业务发生时的仓库名称快照"),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "采购单主键"),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联供应商主键"),
                    supplier_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "入库发生时的供应商名称快照"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联客户主键"),
                    customer_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "业务发生时的客户名称快照"),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "所属部门主键"),
                    department_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "业务发生时的部门名称快照"),
                    purchaser_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "负责采购的采购员主键"),
                    purchaser_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "采购业务发生时的采购员名称快照"),
                    purchase_pattern = table.Column<int>(type: "integer", nullable: true, comment: "采购模式：供应商直供或市场自采"),
                    in_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "计划或实际入库时间（UTC）"),
                    expected_arrival_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "预计到货时间（UTC）"),
                    total_base_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "库存单据基础单位数量合计，仅用于展示"),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "库存单据金额合计，按系统业务币种计量"),
                    print_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "入库单打印状态：0 未打印，1 已打印"),
                    audit_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "执行审核的系统用户主键"),
                    audit_user_name_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "审核时的用户名称快照"),
                    audit_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "审核动作发生时间（UTC）"),
                    reverse_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "执行反审核的系统用户主键"),
                    reverse_user_name_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "反审核时的用户名称快照"),
                    reverse_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "最近一次反审核完成时间（UTC）"),
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
                    table.PrimaryKey("PK_stock_in_order", x => x.id);
                    table.CheckConstraint("ck_stock_in_order_total_amount", "total_amount >= 0");
                    table.CheckConstraint("ck_stock_in_order_total_quantity", "total_base_quantity >= 0");
                    table.ForeignKey(
                        name: "FK_stock_in_order_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stock_in_order_purchase_order_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_in_order_purchaser_purchaser_id",
                        column: x => x.purchaser_id,
                        principalTable: "purchaser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stock_in_order_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stock_in_order_sys_department_department_id",
                        column: x => x.department_id,
                        principalTable: "sys_department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stock_in_order_ware_ware_id",
                        column: x => x.ware_id,
                        principalTable: "ware",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "入库单，记录采购、其他或销售退货入库及审核状态");

            migrationBuilder.CreateTable(
                name: "stock_out_order",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    out_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "出库单业务唯一编号"),
                    order_type = table.Column<int>(type: "integer", nullable: false, comment: "库存单据业务类型"),
                    business_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "出库单状态：草稿、待审核、已审核、已反审核或已删除"),
                    ware_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联仓库主键"),
                    ware_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "库存业务发生时的仓库名称快照"),
                    sale_order_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源销售订单主键"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联客户主键"),
                    customer_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "业务发生时的客户名称快照"),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联供应商主键"),
                    supplier_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "出库发生时的供应商名称快照"),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "所属部门主键"),
                    department_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "业务发生时的部门名称快照"),
                    out_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "计划或实际出库时间（UTC）"),
                    total_base_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "库存单据基础单位数量合计，仅用于展示"),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "库存单据金额合计，按系统业务币种计量"),
                    print_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "出库单打印状态：0 未打印，1 已打印"),
                    audit_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "执行审核的系统用户主键"),
                    audit_user_name_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "审核时的用户名称快照"),
                    audit_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "审核动作发生时间（UTC）"),
                    reverse_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "执行反审核的系统用户主键"),
                    reverse_user_name_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "反审核时的用户名称快照"),
                    reverse_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "最近一次反审核完成时间（UTC）"),
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
                    table.PrimaryKey("PK_stock_out_order", x => x.id);
                    table.CheckConstraint("ck_stock_out_order_total_amount", "total_amount >= 0");
                    table.CheckConstraint("ck_stock_out_order_total_quantity", "total_base_quantity >= 0");
                    table.ForeignKey(
                        name: "FK_stock_out_order_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stock_out_order_sale_order_sale_order_id",
                        column: x => x.sale_order_id,
                        principalTable: "sale_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_out_order_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stock_out_order_sys_department_department_id",
                        column: x => x.department_id,
                        principalTable: "sys_department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stock_out_order_ware_ware_id",
                        column: x => x.ware_id,
                        principalTable: "ware",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "出库单，记录销售、采购退货或其他出库及审核状态");

            migrationBuilder.CreateTable(
                name: "stocktaking_order",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    stocktaking_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "库存盘点单业务唯一编号"),
                    business_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "盘点单状态：草稿、待审核、已审核、已反审核或已删除"),
                    ware_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联仓库主键"),
                    ware_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "库存业务发生时的仓库名称快照"),
                    stocktaking_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "盘点库存快照生成时间（UTC）"),
                    total_book_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "盘点账面数量合计，按各商品基础单位分别求和"),
                    total_actual_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "盘点实盘数量合计，按各商品基础单位分别求和"),
                    total_difference_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "盘点实盘数减账面数的差异合计"),
                    is_adjustment_applied = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "盘点差异是否已生成库存流水"),
                    adjustment_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "盘点差异流水生成完成时间（UTC）"),
                    audit_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "执行审核的系统用户主键"),
                    audit_user_name_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "审核时的用户名称快照"),
                    audit_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "审核动作发生时间（UTC）"),
                    reverse_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "执行反审核的系统用户主键"),
                    reverse_user_name_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "反审核时的用户名称快照"),
                    reverse_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "最近一次反审核完成时间（UTC）"),
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
                    table.PrimaryKey("PK_stocktaking_order", x => x.id);
                    table.ForeignKey(
                        name: "FK_stocktaking_order_ware_ware_id",
                        column: x => x.ware_id,
                        principalTable: "ware",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "库存盘点单，记录仓库盘点时点、账实汇总和调整状态");

            migrationBuilder.CreateTable(
                name: "stock_ledger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    stock_batch_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联库存批次主键"),
                    ware_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联仓库主键"),
                    ware_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "库存业务发生时的仓库名称快照"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务发生时的商品编码快照"),
                    batch_no_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "库存业务发生时的批次号快照"),
                    base_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "业务发生时的基础单位名称快照"),
                    direction = table.Column<int>(type: "integer", nullable: false, comment: "库存流水增减方向"),
                    source_type = table.Column<int>(type: "integer", nullable: false, comment: "库存流水业务来源类型"),
                    source_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "库存流水来源业务主单主键"),
                    source_detail_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "库存流水来源业务明细主键"),
                    change_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "库存流水变更数量，按基础单位计量且必须为正数"),
                    balance_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "库存流水生效后的批次账面数量，按基础单位计量"),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "库存单位成本，按系统业务币种和基础单位计量"),
                    total_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "库存变更数量与单位成本计算后的金额快照"),
                    occurred_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "库存变更实际生效时间（UTC）"),
                    reversed_from_ledger_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "被当前反向流水回滚的原流水主键"),
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
                    table.PrimaryKey("PK_stock_ledger", x => x.id);
                    table.CheckConstraint("ck_stock_ledger_change_quantity", "change_quantity > 0");
                    table.ForeignKey(
                        name: "FK_stock_ledger_stock_batch_stock_batch_id",
                        column: x => x.stock_batch_id,
                        principalTable: "stock_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_ledger_stock_ledger_reversed_from_ledger_id",
                        column: x => x.reversed_from_ledger_id,
                        principalTable: "stock_ledger",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "库存流水，记录审核与反审核对批次余额的每次影响");

            migrationBuilder.CreateTable(
                name: "stock_in_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    stock_in_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属入库主单主键"),
                    purchase_order_detail_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "采购单商品明细主键"),
                    stock_batch_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联库存批次主键"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务发生时的商品编码快照"),
                    goods_unit_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "入库计量单位主键"),
                    goods_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "入库发生时的计量单位名称快照"),
                    conversion_rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "当前单位换算为基础单位的比例"),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "业务数量，按当前商品单位计量"),
                    base_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "按商品基础单位换算后的数量"),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "当前商品单位对应的单价"),
                    total_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "数量与单价计算后的总金额"),
                    batch_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "仓库商品批次号"),
                    product_date = table.Column<DateOnly>(type: "date", nullable: true, comment: "入库商品生产日期，仅记录自然日"),
                    expire_date = table.Column<DateOnly>(type: "date", nullable: true, comment: "商品到期日期，仅记录自然日"),
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
                    table.PrimaryKey("PK_stock_in_detail", x => x.id);
                    table.CheckConstraint("ck_stock_in_detail_conversion_rate", "conversion_rate > 0");
                    table.CheckConstraint("ck_stock_in_detail_price", "unit_price >= 0 AND total_price >= 0");
                    table.CheckConstraint("ck_stock_in_detail_quantity", "quantity > 0 AND base_quantity > 0");
                    table.ForeignKey(
                        name: "FK_stock_in_detail_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_in_detail_goods_unit_goods_unit_id",
                        column: x => x.goods_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_in_detail_purchase_order_detail_purchase_order_detail~",
                        column: x => x.purchase_order_detail_id,
                        principalTable: "purchase_order_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_in_detail_stock_batch_stock_batch_id",
                        column: x => x.stock_batch_id,
                        principalTable: "stock_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_in_detail_stock_in_order_stock_in_order_id",
                        column: x => x.stock_in_order_id,
                        principalTable: "stock_in_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "入库商品明细，记录商品单位、批次、数量和成本快照");

            migrationBuilder.CreateTable(
                name: "stock_out_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    stock_out_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属出库主单主键"),
                    sale_order_detail_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源销售订单商品明细主键"),
                    stock_batch_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联库存批次主键"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务发生时的商品编码快照"),
                    goods_unit_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "出库计量单位主键"),
                    goods_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "出库发生时的计量单位名称快照"),
                    conversion_rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "当前单位换算为基础单位的比例"),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "业务数量，按当前商品单位计量"),
                    base_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "按商品基础单位换算后的数量"),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "当前商品单位对应的单价"),
                    total_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "数量与单价计算后的总金额"),
                    batch_no_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "库存业务发生时的批次号快照"),
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
                    table.PrimaryKey("PK_stock_out_detail", x => x.id);
                    table.CheckConstraint("ck_stock_out_detail_conversion_rate", "conversion_rate > 0");
                    table.CheckConstraint("ck_stock_out_detail_price", "unit_price >= 0 AND total_price >= 0");
                    table.CheckConstraint("ck_stock_out_detail_quantity", "quantity > 0 AND base_quantity > 0");
                    table.ForeignKey(
                        name: "FK_stock_out_detail_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_out_detail_goods_unit_goods_unit_id",
                        column: x => x.goods_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_out_detail_sale_order_detail_sale_order_detail_id",
                        column: x => x.sale_order_detail_id,
                        principalTable: "sale_order_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_out_detail_stock_batch_stock_batch_id",
                        column: x => x.stock_batch_id,
                        principalTable: "stock_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_out_detail_stock_out_order_stock_out_order_id",
                        column: x => x.stock_out_order_id,
                        principalTable: "stock_out_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "出库商品明细，记录商品单位、扣减批次、数量和价格快照");

            migrationBuilder.CreateTable(
                name: "stocktaking_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    stocktaking_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属库存盘点主单主键"),
                    stock_batch_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联库存批次主键"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务发生时的商品编码快照"),
                    batch_no_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "库存业务发生时的批次号快照"),
                    base_unit_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "商品基础单位主键"),
                    base_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "业务发生时的基础单位名称快照"),
                    book_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "盘点快照时的账面数量，按基础单位计量"),
                    actual_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "盘点实际数量，按商品基础单位计量"),
                    difference_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "盘点实盘数量减账面数量，正数盘盈、负数盘亏"),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "库存单位成本，按系统业务币种和基础单位计量"),
                    difference_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "盘点差异数量与单位成本计算后的金额快照"),
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
                    table.PrimaryKey("PK_stocktaking_detail", x => x.id);
                    table.CheckConstraint("ck_stocktaking_detail_actual_quantity", "actual_quantity >= 0");
                    table.CheckConstraint("ck_stocktaking_detail_book_quantity", "book_quantity >= 0");
                    table.CheckConstraint("ck_stocktaking_detail_unit_cost", "unit_cost >= 0");
                    table.ForeignKey(
                        name: "FK_stocktaking_detail_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stocktaking_detail_goods_unit_base_unit_id",
                        column: x => x.base_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stocktaking_detail_stock_batch_stock_batch_id",
                        column: x => x.stock_batch_id,
                        principalTable: "stock_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stocktaking_detail_stocktaking_order_stocktaking_order_id",
                        column: x => x.stocktaking_order_id,
                        principalTable: "stocktaking_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "库存盘点明细，记录批次账面数、实盘数和差异成本");

            migrationBuilder.CreateIndex(
                name: "idx_stock_batch_goods_expire",
                table: "stock_batch",
                columns: new[] { "goods_id", "expire_date" });

            migrationBuilder.CreateIndex(
                name: "idx_stock_batch_ware_goods_batch",
                table: "stock_batch",
                columns: new[] { "ware_id", "goods_id", "batch_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_batch_base_unit_id",
                table: "stock_batch",
                column: "base_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_in_detail_batch_id",
                table: "stock_in_detail",
                column: "stock_batch_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_in_detail_goods_id",
                table: "stock_in_detail",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_in_detail_order_id",
                table: "stock_in_detail",
                column: "stock_in_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_in_detail_purchase_detail_id",
                table: "stock_in_detail",
                column: "purchase_order_detail_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_detail_goods_unit_id",
                table: "stock_in_detail",
                column: "goods_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_in_order_in_no",
                table: "stock_in_order",
                column: "in_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_stock_in_order_purchase_order_id",
                table: "stock_in_order",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_in_order_ware_status_time",
                table: "stock_in_order",
                columns: new[] { "ware_id", "business_status", "in_time" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_order_customer_id",
                table: "stock_in_order",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_order_department_id",
                table: "stock_in_order",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_order_purchaser_id",
                table: "stock_in_order",
                column: "purchaser_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_order_supplier_id",
                table: "stock_in_order",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_ledger_batch_time",
                table: "stock_ledger",
                columns: new[] { "stock_batch_id", "occurred_time" });

            migrationBuilder.CreateIndex(
                name: "idx_stock_ledger_reversed_from",
                table: "stock_ledger",
                column: "reversed_from_ledger_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_stock_ledger_source",
                table: "stock_ledger",
                columns: new[] { "source_order_id", "source_detail_id" });

            migrationBuilder.CreateIndex(
                name: "idx_stock_out_detail_batch_id",
                table: "stock_out_detail",
                column: "stock_batch_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_out_detail_goods_id",
                table: "stock_out_detail",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_out_detail_order_id",
                table: "stock_out_detail",
                column: "stock_out_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_out_detail_sale_detail_id",
                table: "stock_out_detail",
                column: "sale_order_detail_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_out_detail_goods_unit_id",
                table: "stock_out_detail",
                column: "goods_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_out_order_out_no",
                table: "stock_out_order",
                column: "out_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_stock_out_order_sale_order_id",
                table: "stock_out_order",
                column: "sale_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_out_order_ware_status_time",
                table: "stock_out_order",
                columns: new[] { "ware_id", "business_status", "out_time" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_out_order_customer_id",
                table: "stock_out_order",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_out_order_department_id",
                table: "stock_out_order",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_out_order_supplier_id",
                table: "stock_out_order",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_stocktaking_detail_goods_id",
                table: "stocktaking_detail",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_stocktaking_detail_order_batch",
                table: "stocktaking_detail",
                columns: new[] { "stocktaking_order_id", "stock_batch_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stocktaking_detail_base_unit_id",
                table: "stocktaking_detail",
                column: "base_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocktaking_detail_stock_batch_id",
                table: "stocktaking_detail",
                column: "stock_batch_id");

            migrationBuilder.CreateIndex(
                name: "idx_stocktaking_order_no",
                table: "stocktaking_order",
                column: "stocktaking_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_stocktaking_order_ware_status_time",
                table: "stocktaking_order",
                columns: new[] { "ware_id", "business_status", "stocktaking_time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_in_detail");

            migrationBuilder.DropTable(
                name: "stock_ledger");

            migrationBuilder.DropTable(
                name: "stock_out_detail");

            migrationBuilder.DropTable(
                name: "stocktaking_detail");

            migrationBuilder.DropTable(
                name: "stock_in_order");

            migrationBuilder.DropTable(
                name: "stock_out_order");

            migrationBuilder.DropTable(
                name: "stock_batch");

            migrationBuilder.DropTable(
                name: "stocktaking_order");
        }
    }
}
