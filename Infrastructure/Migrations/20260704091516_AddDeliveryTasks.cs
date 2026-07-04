using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "delivery_task_id",
                table: "delivery_exception",
                type: "uuid",
                nullable: true,
                comment: "关联配送任务主键");

            migrationBuilder.CreateTable(
                name: "delivery_task",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "记录主键"),
                    task_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "配送任务业务唯一编号"),
                    stock_out_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源销售出库单主键，同一出库单只能生成一条配送任务"),
                    sale_order_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "来源销售订单主键"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联客户主键"),
                    customer_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "业务发生时的客户名称快照"),
                    contact_name_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "业务发生时的联系人姓名快照"),
                    contact_phone_snapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "业务发生时的联系人电话快照"),
                    delivery_address_snapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "下单时的配送地址快照"),
                    ware_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "关联仓库主键"),
                    ware_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false, comment: "库存业务发生时的仓库名称快照"),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联司机主键"),
                    driver_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "配送任务分配时的司机姓名快照"),
                    driver_phone_snapshot = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "配送任务分配时的司机电话快照"),
                    carrier_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "所属承运商主键"),
                    carrier_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "配送任务分配时的承运商名称快照"),
                    route_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "关联配送路线主键"),
                    route_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "配送任务规划时的路线名称快照"),
                    route_sequence = table.Column<int>(type: "integer", nullable: true, comment: "客户在配送路线内的执行顺序"),
                    delivery_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "配送任务状态：待分配、已分配、配送中、异常或已签收"),
                    out_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "计划或实际出库时间（UTC）"),
                    assigned_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "最近一次分配司机的时间（UTC）"),
                    planned_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "最近一次路线规划时间（UTC）"),
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
                    table.PrimaryKey("PK_delivery_task", x => x.id);
                    table.ForeignKey(
                        name: "FK_delivery_task_carrier_carrier_id",
                        column: x => x.carrier_id,
                        principalTable: "carrier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_delivery_task_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_task_delivery_route_route_id",
                        column: x => x.route_id,
                        principalTable: "delivery_route",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_delivery_task_driver_driver_id",
                        column: x => x.driver_id,
                        principalTable: "driver",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_task_sale_order_sale_order_id",
                        column: x => x.sale_order_id,
                        principalTable: "sale_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_task_stock_out_order_stock_out_order_id",
                        column: x => x.stock_out_order_id,
                        principalTable: "stock_out_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_task_ware_ware_id",
                        column: x => x.ware_id,
                        principalTable: "ware",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "配送任务，记录销售出库后的客户、司机、路线和履约状态");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_exception_task_id",
                table: "delivery_exception",
                column: "delivery_task_id");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_task_driver_status",
                table: "delivery_task",
                columns: new[] { "driver_id", "delivery_status" });

            migrationBuilder.CreateIndex(
                name: "idx_delivery_task_no",
                table: "delivery_task",
                column: "task_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_delivery_task_route_sequence",
                table: "delivery_task",
                columns: new[] { "route_id", "route_sequence" });

            migrationBuilder.CreateIndex(
                name: "idx_delivery_task_status_out_time",
                table: "delivery_task",
                columns: new[] { "delivery_status", "out_time" });

            migrationBuilder.CreateIndex(
                name: "idx_delivery_task_stock_out_order_id",
                table: "delivery_task",
                column: "stock_out_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_delivery_task_carrier_id",
                table: "delivery_task",
                column: "carrier_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_task_customer_id",
                table: "delivery_task",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_task_sale_order_id",
                table: "delivery_task",
                column: "sale_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_task_ware_id",
                table: "delivery_task",
                column: "ware_id");

            migrationBuilder.AddForeignKey(
                name: "FK_delivery_exception_delivery_task_delivery_task_id",
                table: "delivery_exception",
                column: "delivery_task_id",
                principalTable: "delivery_task",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_delivery_exception_delivery_task_delivery_task_id",
                table: "delivery_exception");

            migrationBuilder.DropTable(
                name: "delivery_task");

            migrationBuilder.DropIndex(
                name: "idx_delivery_exception_task_id",
                table: "delivery_exception");

            migrationBuilder.DropColumn(
                name: "delivery_task_id",
                table: "delivery_exception");
        }
    }
}
