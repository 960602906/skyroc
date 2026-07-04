using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "signed_time",
                table: "delivery_task",
                type: "timestamp with time zone",
                nullable: true,
                comment: "客户完成配送签收的时间（UTC）");

            migrationBuilder.AddColumn<DateTime>(
                name: "started_time",
                table: "delivery_task",
                type: "timestamp with time zone",
                nullable: true,
                comment: "配送任务开始执行的时间（UTC）");

            migrationBuilder.CreateTable(
                name: "order_receipt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    receipt_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "签收回单业务唯一编号"),
                    delivery_task_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "对应配送任务主键，每个配送任务只能产生一张签收回单"),
                    sale_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源销售订单主键"),
                    stock_out_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "对应销售出库单主键，用于追溯本次实际交付商品"),
                    signer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "客户侧实际签收人姓名"),
                    signed_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "客户完成配送签收的时间（UTC）"),
                    sign_remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "签收时记录的交付说明或客户意见"),
                    receipt_image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "纸质扫描件或电子回单的可访问地址"),
                    returned_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "回单资料确认归档的时间（UTC）"),
                    return_remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "回单归档说明"),
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
                    table.PrimaryKey("PK_order_receipt", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_receipt_delivery_task_delivery_task_id",
                        column: x => x.delivery_task_id,
                        principalTable: "delivery_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_receipt_sale_order_sale_order_id",
                        column: x => x.sale_order_id,
                        principalTable: "sale_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_receipt_stock_out_order_stock_out_order_id",
                        column: x => x.stock_out_order_id,
                        principalTable: "stock_out_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "订单签收回单，按配送任务记录客户签收、回单归档和验收结果");

            migrationBuilder.CreateTable(
                name: "order_check_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    order_receipt_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "所属签收回单主键"),
                    sale_order_detail_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源销售订单商品明细主键"),
                    stock_out_detail_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "本次配送对应的销售出库商品明细主键"),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联商品主键"),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的商品名称快照"),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "业务发生时的商品编码快照"),
                    goods_unit_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "本次出库使用的计量单位主键"),
                    goods_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "本次出库使用的计量单位名称快照"),
                    delivered_base_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "本次实际配送数量，按商品基础单位计量"),
                    accepted_base_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false, comment: "客户实际确认数量，按商品基础单位计量"),
                    check_status = table.Column<int>(type: "integer", nullable: false, comment: "客户验收结论：通过或拒绝"),
                    accepted_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false, comment: "按客户确认数量和出库价格计算的验收金额"),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "当前商品行的验收差异或拒收原因"),
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
                    table.PrimaryKey("PK_order_check_detail", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_check_detail_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_check_detail_goods_unit_goods_unit_id",
                        column: x => x.goods_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_check_detail_order_receipt_order_receipt_id",
                        column: x => x.order_receipt_id,
                        principalTable: "order_receipt",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_check_detail_sale_order_detail_sale_order_detail_id",
                        column: x => x.sale_order_detail_id,
                        principalTable: "sale_order_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_check_detail_stock_out_detail_stock_out_detail_id",
                        column: x => x.stock_out_detail_id,
                        principalTable: "stock_out_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "订单商品验收明细，记录每个销售出库商品行的交付和客户确认结果");

            migrationBuilder.CreateIndex(
                name: "idx_order_check_receipt_stock_out_detail",
                table: "order_check_detail",
                columns: new[] { "order_receipt_id", "stock_out_detail_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_order_check_sale_order_detail_id",
                table: "order_check_detail",
                column: "sale_order_detail_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_check_detail_goods_id",
                table: "order_check_detail",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_check_detail_goods_unit_id",
                table: "order_check_detail",
                column: "goods_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_check_detail_stock_out_detail_id",
                table: "order_check_detail",
                column: "stock_out_detail_id");

            migrationBuilder.CreateIndex(
                name: "idx_order_receipt_delivery_task_id",
                table: "order_receipt",
                column: "delivery_task_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_order_receipt_no",
                table: "order_receipt",
                column: "receipt_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_order_receipt_order_returned_time",
                table: "order_receipt",
                columns: new[] { "sale_order_id", "returned_time" });

            migrationBuilder.CreateIndex(
                name: "IX_order_receipt_stock_out_order_id",
                table: "order_receipt",
                column: "stock_out_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_check_detail");

            migrationBuilder.DropTable(
                name: "order_receipt");

            migrationBuilder.DropColumn(
                name: "signed_time",
                table: "delivery_task");

            migrationBuilder.DropColumn(
                name: "started_time",
                table: "delivery_task");
        }
    }
}
