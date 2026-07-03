using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchasePlanTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchase_plan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    plan_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    plan_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    purchase_pattern = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    purchase_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supplier_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    purchaser_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purchaser_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
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
                    table.PrimaryKey("PK_purchase_plan", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_plan_purchaser_purchaser_id",
                        column: x => x.purchaser_id,
                        principalTable: "purchaser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchase_plan_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "purchase_plan_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    purchase_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_name_snapshot = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    goods_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    purchase_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_unit_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    required_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    planned_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    purchased_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
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
                    table.PrimaryKey("PK_purchase_plan_detail", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_plan_detail_goods_goods_id",
                        column: x => x.goods_id,
                        principalTable: "goods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_plan_detail_goods_unit_purchase_unit_id",
                        column: x => x.purchase_unit_id,
                        principalTable: "goods_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_plan_detail_purchase_plan_purchase_plan_id",
                        column: x => x.purchase_plan_id,
                        principalTable: "purchase_plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_plan_order_rel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    purchase_plan_detail_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_order_detail_id = table.Column<Guid>(type: "uuid", nullable: false),
                    required_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
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
                    table.PrimaryKey("PK_purchase_plan_order_rel", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_plan_order_rel_purchase_plan_detail_purchase_plan_~",
                        column: x => x.purchase_plan_detail_id,
                        principalTable: "purchase_plan_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_plan_order_rel_sale_order_detail_sale_order_detail~",
                        column: x => x.sale_order_detail_id,
                        principalTable: "sale_order_detail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_plan_order_rel_sale_order_sale_order_id",
                        column: x => x.sale_order_id,
                        principalTable: "sale_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_purchase_plan_date_status",
                table: "purchase_plan",
                columns: new[] { "plan_date", "purchase_status" });

            migrationBuilder.CreateIndex(
                name: "idx_purchase_plan_plan_no",
                table: "purchase_plan",
                column: "plan_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_purchase_plan_purchaser_id",
                table: "purchase_plan",
                column: "purchaser_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_plan_supplier_id",
                table: "purchase_plan",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_plan_detail_goods_id",
                table: "purchase_plan_detail",
                column: "goods_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_plan_detail_plan_id",
                table: "purchase_plan_detail",
                column: "purchase_plan_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_plan_detail_unit_id",
                table: "purchase_plan_detail",
                column: "purchase_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_plan_order_rel_detail_source",
                table: "purchase_plan_order_rel",
                columns: new[] { "purchase_plan_detail_id", "sale_order_detail_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_purchase_plan_order_rel_order_detail_id",
                table: "purchase_plan_order_rel",
                column: "sale_order_detail_id");

            migrationBuilder.CreateIndex(
                name: "idx_purchase_plan_order_rel_order_id",
                table: "purchase_plan_order_rel",
                column: "sale_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_plan_order_rel");

            migrationBuilder.DropTable(
                name: "purchase_plan_detail");

            migrationBuilder.DropTable(
                name: "purchase_plan");
        }
    }
}
