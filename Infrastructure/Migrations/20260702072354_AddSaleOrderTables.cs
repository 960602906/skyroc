using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleOrderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sale_order",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    customer_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quotation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ware_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    receive_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    out_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    order_status = table.Column<int>(type: "integer", nullable: false, defaultValue: -1),
                    return_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    print_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    out_storage_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    order_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    settlement_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    has_out_sale = table.Column<bool>(type: "boolean", nullable: false),
                    update_status = table.Column<bool>(type: "boolean", nullable: false),
                    has_purchase_plan = table.Column<bool>(type: "boolean", nullable: false),
                    contact_name_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    contact_phone_snapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    delivery_address_snapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    inner_remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_order", x => x.id);
                    table.ForeignKey(
                        name: "FK_sale_order_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_order_quotation_quotation_id",
                        column: x => x.quotation_id,
                        principalTable: "quotation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sale_order_ware_ware_id",
                        column: x => x.ware_id,
                        principalTable: "ware",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "order_audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sale_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    previous_status = table.Column<int>(type: "integer", nullable: false),
                    current_status = table.Column<int>(type: "integer", nullable: false),
                    audit_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_user_name_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    audit_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_audit_log_sale_order_sale_order_id",
                        column: x => x.sale_order_id,
                        principalTable: "sale_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_audit_log_sys_user_audit_user_id",
                        column: x => x.audit_user_id,
                        principalSchema: "public",
                        principalTable: "sys_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "sale_order_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sale_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    goods_image_snapshot = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    goods_type_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    goods_description_snapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    goods_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    base_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    base_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    base_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    unit_conversion = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    fixed_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    fixed_goods_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fixed_goods_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    total_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    inner_remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    customer_check_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    customer_check_base_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    customer_check_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    has_purchase_plan = table.Column<bool>(type: "boolean", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    create_by = table.Column<Guid>(type: "uuid", nullable: true),
                    create_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_by = table.Column<Guid>(type: "uuid", nullable: true),
                    update_name = table.Column<string>(type: "varchar(50)", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_order_detail", x => x.id);
                    table.ForeignKey(
                        name: "FK_sale_order_detail_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_order_detail_goods_unit_base_unit_id",
                        column: x => x.base_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sale_order_detail_goods_unit_fixed_goods_unit_id",
                        column: x => x.fixed_goods_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_sale_order_detail_goods_unit_goods_unit_id",
                        column: x => x.goods_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_order_detail_sale_order_sale_order_id",
                        column: x => x.sale_order_id,
                        principalTable: "sale_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_order_audit_log_order_time",
                table: "order_audit_log",
                columns: new[] { "sale_order_id", "audit_time" });

            migrationBuilder.CreateIndex(
                name: "idx_order_audit_log_user_id",
                table: "order_audit_log",
                column: "audit_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_sale_order_customer_id",
                table: "sale_order",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_sale_order_date_status",
                table: "sale_order",
                columns: new[] { "order_date", "order_status" });

            migrationBuilder.CreateIndex(
                name: "idx_sale_order_order_no",
                table: "sale_order",
                column: "order_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_sale_order_quotation_id",
                table: "sale_order",
                column: "quotation_id");

            migrationBuilder.CreateIndex(
                name: "idx_sale_order_ware_id",
                table: "sale_order",
                column: "ware_id");

            migrationBuilder.CreateIndex(
                name: "idx_sale_order_detail_goods_id",
                table: "sale_order_detail",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_sale_order_detail_order_id",
                table: "sale_order_detail",
                column: "sale_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_sale_order_detail_unit_id",
                table: "sale_order_detail",
                column: "goods_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_order_detail_base_unit_id",
                table: "sale_order_detail",
                column: "base_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_order_detail_fixed_goods_unit_id",
                table: "sale_order_detail",
                column: "fixed_goods_unit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_audit_log");

            migrationBuilder.DropTable(
                name: "sale_order_detail");

            migrationBuilder.DropTable(
                name: "sale_order");
        }
    }
}
