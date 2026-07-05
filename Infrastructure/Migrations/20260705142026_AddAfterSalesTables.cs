using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAfterSalesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "after_sale",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    after_sale_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "售后单业务唯一编号"),
                    sale_order_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源销售订单主键"),
                    sale_order_no_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "售后建单时的销售订单编号快照"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联客户主键"),
                    customer_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的客户名称快照"),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "售后来源标识"),
                    after_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "售后单状态：待提交、待审核、待退货、待退款或已完成"),
                    order_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "售后建单时的原订单金额，按系统业务币种计量"),
                    settlement_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "售后处理后的客户结算金额，按系统业务币种计量"),
                    contact_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "业务发生时的联系人姓名快照"),
                    contact_phone_snapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "业务发生时的联系人电话快照"),
                    pickup_address_snapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "售后取货地址快照"),
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
                    table.PrimaryKey("PK_after_sale", x => x.id);
                    table.CheckConstraint("ck_after_sale_amounts", "order_price >= 0 AND settlement_price >= 0");
                    table.CheckConstraint("ck_after_sale_status", "after_status BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_after_sale_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_after_sale_sale_order_sale_order_id",
                        column: x => x.sale_order_id,
                        principalTable: "sale_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "售后单，记录来源订单、客户、审核状态和结算金额快照");

            migrationBuilder.CreateTable(
                name: "after_sale_audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    after_sale_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属售后单主键"),
                    action = table.Column<int>(type: "integer", nullable: false, comment: "售后审核轨迹动作：提交、通过、驳回、重提或反审核"),
                    previous_status = table.Column<int>(type: "integer", nullable: false, comment: "审核动作发生前的售后单状态"),
                    current_status = table.Column<int>(type: "integer", nullable: false, comment: "审核动作完成后的售后单状态"),
                    audit_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "执行审核的系统用户主键"),
                    audit_user_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "审核时的用户名称快照"),
                    audit_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "审核动作发生时间（UTC）"),
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
                    table.PrimaryKey("PK_after_sale_audit_log", x => x.id);
                    table.CheckConstraint("ck_after_sale_audit_action", "action BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_after_sale_audit_statuses", "previous_status BETWEEN 1 AND 5 AND current_status BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_after_sale_audit_log_after_sale_after_sale_id",
                        column: x => x.after_sale_id,
                        principalTable: "after_sale",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_after_sale_audit_log_sys_user_audit_user_id",
                        column: x => x.audit_user_id,
                        principalSchema: "public",
                        principalTable: "sys_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "售后审核记录，保存提交、审核、驳回、重提和反审核轨迹");

            migrationBuilder.CreateTable(
                name: "after_sale_goods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    after_sale_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属售后单主键"),
                    sale_order_detail_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "来源销售订单商品明细主键；手工录入时可为空"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务发生时的商品编码快照"),
                    goods_type_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "业务发生时的商品分类名称快照"),
                    goods_unit_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "申请退款或退货所使用的商品单位主键"),
                    goods_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "售后建单时的申请单位名称快照"),
                    base_unit_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "商品基础单位主键"),
                    base_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "业务发生时的基础单位名称快照"),
                    conversion_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "当前单位换算为基础单位的比例"),
                    after_sale_type = table.Column<int>(type: "integer", nullable: false, comment: "售后申请类型：仅退款或退货退款"),
                    actual_refund_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "最终批准退款或退货的数量，按申请商品单位计量"),
                    base_refund_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false, comment: "最终批准数量换算到商品基础单位后的数量"),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "售后核算采用的订单单价快照"),
                    refund_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "当前售后商品最终退款或减免金额"),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联供应商主键"),
                    supplier_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "售后建单时的原商品供应商名称快照"),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "所属部门主键"),
                    department_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "售后建单或定责时的部门名称快照"),
                    reason_type = table.Column<int>(type: "integer", nullable: false, comment: "售后原因分类"),
                    handle_type = table.Column<int>(type: "integer", nullable: false, comment: "售后处理方式：减免、补货、换货、账单核算、客户沟通或其他"),
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
                    table.PrimaryKey("PK_after_sale_goods", x => x.id);
                    table.UniqueConstraint("ak_after_sale_goods_id_after_sale_id", x => new { x.id, x.after_sale_id });
                    table.CheckConstraint("ck_after_sale_goods_amounts", "unit_price >= 0 AND refund_amount >= 0");
                    table.CheckConstraint("ck_after_sale_goods_conversion_rate", "conversion_rate > 0");
                    table.CheckConstraint("ck_after_sale_goods_handle", "handle_type BETWEEN 1 AND 6");
                    table.CheckConstraint("ck_after_sale_goods_quantities", "actual_refund_quantity > 0 AND base_refund_quantity > 0");
                    table.CheckConstraint("ck_after_sale_goods_reason", "reason_type BETWEEN 1 AND 13");
                    table.CheckConstraint("ck_after_sale_goods_type", "after_sale_type IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_after_sale_goods_after_sale_after_sale_id",
                        column: x => x.after_sale_id,
                        principalTable: "after_sale",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_after_sale_goods_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_after_sale_goods_goods_unit_base_unit_id",
                        column: x => x.base_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_after_sale_goods_goods_unit_goods_unit_id",
                        column: x => x.goods_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_after_sale_goods_sale_order_detail_sale_order_detail_id",
                        column: x => x.sale_order_detail_id,
                        principalTable: "sale_order_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_after_sale_goods_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_after_sale_goods_sys_department_department_id",
                        column: x => x.department_id,
                        principalTable: "sys_department",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "售后商品明细，记录商品、单位、原因、处理方式和退款金额快照");

            migrationBuilder.CreateTable(
                name: "pickup_task",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    task_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "售后取货任务业务唯一编号"),
                    after_sale_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属售后单主键，必须与关联售后商品的所属售后单一致"),
                    after_sale_goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "需要回收商品的售后商品明细主键"),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联司机主键"),
                    driver_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "取货任务最近一次分配时的司机姓名快照"),
                    driver_phone_snapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "取货任务最近一次分配时的司机电话快照"),
                    contact_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "业务发生时的联系人姓名快照"),
                    contact_phone_snapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "业务发生时的联系人电话快照"),
                    pickup_address_snapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "售后取货地址快照"),
                    pickup_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "取货任务状态：待分配、待取货、取货中、已完成或已取消"),
                    planned_pickup_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "计划上门取货时间（UTC）"),
                    assigned_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "取货任务最近一次分配司机的时间（UTC）"),
                    started_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "司机开始执行取货任务的时间（UTC）"),
                    completed_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "任务完成时间（UTC）"),
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
                    table.PrimaryKey("PK_pickup_task", x => x.id);
                    table.CheckConstraint("ck_pickup_task_status", "pickup_status BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_pickup_task_after_sale_after_sale_id",
                        column: x => x.after_sale_id,
                        principalTable: "after_sale",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pickup_task_driver_driver_id",
                        column: x => x.driver_id,
                        principalTable: "driver",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pickup_task_after_sale_goods",
                        columns: x => new { x.after_sale_goods_id, x.after_sale_id },
                        principalTable: "after_sale_goods",
                        principalColumns: new[] { "id", "after_sale_id" },
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "售后取货任务，记录退货商品的司机分配、取货地址和执行状态");

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_customer_id",
                table: "after_sale",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_no",
                table: "after_sale",
                column: "after_sale_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_order_id",
                table: "after_sale",
                column: "sale_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_status_create_time",
                table: "after_sale",
                columns: new[] { "after_status", "create_time" });

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_audit_order_time",
                table: "after_sale_audit_log",
                columns: new[] { "after_sale_id", "audit_time" });

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_audit_user_id",
                table: "after_sale_audit_log",
                column: "audit_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_goods_base_unit_id",
                table: "after_sale_goods",
                column: "base_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_goods_department_id",
                table: "after_sale_goods",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_goods_goods_id",
                table: "after_sale_goods",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_goods_order_detail",
                table: "after_sale_goods",
                columns: new[] { "after_sale_id", "sale_order_detail_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_goods_sale_order_detail_id",
                table: "after_sale_goods",
                column: "sale_order_detail_id");

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_goods_supplier_id",
                table: "after_sale_goods",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_after_sale_goods_unit_id",
                table: "after_sale_goods",
                column: "goods_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_pickup_task_after_sale_goods_id",
                table: "pickup_task",
                column: "after_sale_goods_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_pickup_task_after_sale_status",
                table: "pickup_task",
                columns: new[] { "after_sale_id", "pickup_status" });

            migrationBuilder.CreateIndex(
                name: "idx_pickup_task_driver_status",
                table: "pickup_task",
                columns: new[] { "driver_id", "pickup_status" });

            migrationBuilder.CreateIndex(
                name: "idx_pickup_task_goods_parent",
                table: "pickup_task",
                columns: new[] { "after_sale_goods_id", "after_sale_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_pickup_task_no",
                table: "pickup_task",
                column: "task_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_pickup_task_status_plan_time",
                table: "pickup_task",
                columns: new[] { "pickup_status", "planned_pickup_time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "after_sale_audit_log");

            migrationBuilder.DropTable(
                name: "pickup_task");

            migrationBuilder.DropTable(
                name: "after_sale_goods");

            migrationBuilder.DropTable(
                name: "after_sale");
        }
    }
}
