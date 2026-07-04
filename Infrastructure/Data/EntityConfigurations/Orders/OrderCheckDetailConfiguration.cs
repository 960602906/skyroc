using Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 订单商品验收明细的数据库映射配置。
/// </summary>
public class OrderCheckDetailConfiguration : IEntityTypeConfiguration<OrderCheckDetail>
{
    /// <summary>
    /// 配置来源出库行、商品快照、数量金额精度、唯一约束及业务外键。
    /// </summary>
    /// <param name="builder">商品验收明细实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<OrderCheckDetail> builder)
    {
        builder.ToTable("order_check_detail");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.OrderReceiptId).HasColumnName("order_receipt_id").IsRequired();
        builder.Property(x => x.SaleOrderDetailId).HasColumnName("sale_order_detail_id").IsRequired();
        builder.Property(x => x.StockOutDetailId).HasColumnName("stock_out_detail_id").IsRequired();
        builder.Property(x => x.GoodsId).HasColumnName("goods_id").IsRequired();
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.GoodsUnitId).HasColumnName("goods_unit_id").IsRequired();
        builder.Property(x => x.GoodsUnitNameSnapshot).HasColumnName("goods_unit_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.DeliveredBaseQuantity).HasColumnName("delivered_base_quantity").HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(x => x.AcceptedBaseQuantity).HasColumnName("accepted_base_quantity").HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(x => x.CheckStatus).HasColumnName("check_status").HasColumnType("integer").IsRequired();
        builder.Property(x => x.AcceptedAmount).HasColumnName("accepted_amount").HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => new { x.OrderReceiptId, x.StockOutDetailId })
            .IsUnique()
            .HasDatabaseName("idx_order_check_receipt_stock_out_detail");
        builder.HasIndex(x => x.SaleOrderDetailId).HasDatabaseName("idx_order_check_sale_order_detail_id");

        builder.HasOne(x => x.OrderReceipt)
            .WithMany(x => x.CheckDetails)
            .HasForeignKey(x => x.OrderReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SaleOrderDetail)
            .WithMany()
            .HasForeignKey(x => x.SaleOrderDetailId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockOutDetail)
            .WithMany()
            .HasForeignKey(x => x.StockOutDetailId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Goods)
            .WithMany()
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.GoodsUnit)
            .WithMany()
            .HasForeignKey(x => x.GoodsUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
