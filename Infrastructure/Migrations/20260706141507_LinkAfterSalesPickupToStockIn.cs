using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkAfterSalesPickupToStockIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "after_sale_id",
                table: "stock_in_order",
                type: "uuid",
                nullable: true,
                comment: "来源售后单主键，仅由已完成取货任务生成的销售退货入库填写");

            migrationBuilder.AddColumn<Guid>(
                name: "pickup_task_id",
                table: "stock_in_detail",
                type: "uuid",
                nullable: true,
                comment: "来源售后取货任务主键，同一已完成任务最多生成一条销售退货入库明细");

            migrationBuilder.CreateIndex(
                name: "idx_stock_in_order_after_sale_id",
                table: "stock_in_order",
                column: "after_sale_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_in_detail_pickup_task_id",
                table: "stock_in_detail",
                column: "pickup_task_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_in_detail_pickup_task_pickup_task_id",
                table: "stock_in_detail",
                column: "pickup_task_id",
                principalTable: "pickup_task",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_in_order_after_sale_after_sale_id",
                table: "stock_in_order",
                column: "after_sale_id",
                principalTable: "after_sale",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_in_detail_pickup_task_pickup_task_id",
                table: "stock_in_detail");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_in_order_after_sale_after_sale_id",
                table: "stock_in_order");

            migrationBuilder.DropIndex(
                name: "idx_stock_in_order_after_sale_id",
                table: "stock_in_order");

            migrationBuilder.DropIndex(
                name: "idx_stock_in_detail_pickup_task_id",
                table: "stock_in_detail");

            migrationBuilder.DropColumn(
                name: "after_sale_id",
                table: "stock_in_order");

            migrationBuilder.DropColumn(
                name: "pickup_task_id",
                table: "stock_in_detail");
        }
    }
}
