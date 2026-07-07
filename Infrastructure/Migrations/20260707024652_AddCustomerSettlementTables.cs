using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSettlementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_settlement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    settlement_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "客户结款凭证业务唯一编号"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联客户主键"),
                    customer_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的客户名称快照"),
                    settlement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "客户实际结款日期（UTC）"),
                    serial_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "外部交易流水号"),
                    should_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "结款前待结金额合计，按系统业务币种计量"),
                    payment_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "实际收款金额，按系统业务币种计量"),
                    discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "优惠减免金额，按系统业务币种计量"),
                    applied_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次增加的已结金额，等于收款金额与优惠金额合计"),
                    remaining_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次处理后的剩余待结金额，按系统业务币种计量"),
                    settlement_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "客户结款状态：作废、待结款、部分结款或已结款"),
                    voided_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "凭证作废时间（UTC）"),
                    voided_by = table.Column<Guid>(type: "uuid", nullable: true, comment: "作废操作人主键"),
                    voided_by_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "作废操作人名称快照"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "客户结款或作废备注，记录收款渠道、优惠原因或回滚说明"),
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
                    table.PrimaryKey("PK_customer_settlement", x => x.id);
                    table.CheckConstraint("ck_customer_settlement_amounts", "should_amount >= 0 AND payment_amount >= 0 AND discount_amount >= 0 AND applied_amount = payment_amount + discount_amount AND remaining_amount >= 0");
                    table.CheckConstraint("ck_customer_settlement_status", "settlement_status IN (-1, 1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_customer_settlement_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "客户结款凭证，记录客户付款、优惠和对应账单余额核销结果");

            migrationBuilder.CreateTable(
                name: "customer_settlement_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    customer_settlement_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属客户结款凭证主键"),
                    customer_bill_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "被核销的客户账单主键"),
                    customer_bill_no_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "被核销客户账单编号快照"),
                    sale_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "被核销账单来源销售订单主键"),
                    sale_order_no_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "被核销账单来源销售订单编号快照"),
                    receivable_amount_snapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "结款时账单应收金额快照"),
                    previous_settled_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次结款前账单已结金额快照"),
                    payment_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "实际收款金额，按系统业务币种计量"),
                    discount_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "优惠减免金额，按系统业务币种计量"),
                    applied_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次增加的已结金额，等于收款金额与优惠金额合计"),
                    current_settled_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次结款后账单已结金额快照"),
                    remaining_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, comment: "本次处理后的剩余待结金额，按系统业务币种计量"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "客户结款明细备注，记录单张账单的优惠或差异说明"),
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
                    table.PrimaryKey("PK_customer_settlement_detail", x => x.id);
                    table.CheckConstraint("ck_customer_settlement_detail_amounts", "receivable_amount_snapshot >= 0 AND previous_settled_amount >= 0 AND payment_amount >= 0 AND discount_amount >= 0 AND applied_amount = payment_amount + discount_amount AND current_settled_amount >= previous_settled_amount AND remaining_amount >= 0");
                    table.ForeignKey(
                        name: "FK_customer_settlement_detail_customer_bill_customer_bill_id",
                        column: x => x.customer_bill_id,
                        principalTable: "customer_bill",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_settlement_detail_customer_settlement_customer_set~",
                        column: x => x.customer_settlement_id,
                        principalTable: "customer_settlement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_customer_settlement_detail_sale_order_sale_order_id",
                        column: x => x.sale_order_id,
                        principalTable: "sale_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "客户结款凭证明细，记录单张客户账单在本次结款中的金额变化");

            migrationBuilder.CreateIndex(
                name: "idx_customer_settlement_customer_date",
                table: "customer_settlement",
                columns: new[] { "customer_id", "settlement_date" });

            migrationBuilder.CreateIndex(
                name: "idx_customer_settlement_no",
                table: "customer_settlement",
                column: "settlement_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_settlement_serial_no",
                table: "customer_settlement",
                column: "serial_no");

            migrationBuilder.CreateIndex(
                name: "idx_customer_settlement_status_date",
                table: "customer_settlement",
                columns: new[] { "settlement_status", "settlement_date" });

            migrationBuilder.CreateIndex(
                name: "idx_customer_settlement_detail_bill_id",
                table: "customer_settlement_detail",
                column: "customer_bill_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_settlement_detail_sale_order_id",
                table: "customer_settlement_detail",
                column: "sale_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_customer_settlement_detail_settlement_bill",
                table: "customer_settlement_detail",
                columns: new[] { "customer_settlement_id", "customer_bill_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_settlement_detail");

            migrationBuilder.DropTable(
                name: "customer_settlement");
        }
    }
}
