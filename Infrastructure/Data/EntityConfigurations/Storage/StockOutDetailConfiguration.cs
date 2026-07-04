using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations.Storage;

public class StockOutDetailConfiguration : IEntityTypeConfiguration<StockOutDetail>
{
    public void Configure(EntityTypeBuilder<StockOutDetail> builder)
    {
        builder.ToTable(
            "stock_out_detail",
            table =>
            {
                table.HasCheckConstraint("ck_stock_out_detail_conversion_rate", "conversion_rate > 0");
                table.HasCheckConstraint("ck_stock_out_detail_quantity", "quantity > 0 AND base_quantity > 0");
                table.HasCheckConstraint("ck_stock_out_detail_price", "unit_price >= 0 AND total_price >= 0");
            });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.StockOutOrderId).HasColumnName("stock_out_order_id");
        builder.Property(x => x.SaleOrderDetailId).HasColumnName("sale_order_detail_id");
        builder.Property(x => x.StockBatchId).HasColumnName("stock_batch_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.GoodsUnitId).HasColumnName("goods_unit_id");
        builder.Property(x => x.GoodsUnitNameSnapshot).HasColumnName("goods_unit_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ConversionRate).HasColumnName("conversion_rate").HasColumnType("numeric(18,6)");
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.BaseQuantity).HasColumnName("base_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.TotalPrice).HasColumnName("total_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.BatchNoSnapshot).HasColumnName("batch_no_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.StockOutOrderId).HasDatabaseName("idx_stock_out_detail_order_id");
        builder.HasIndex(x => x.SaleOrderDetailId).HasDatabaseName("idx_stock_out_detail_sale_detail_id");
        builder.HasIndex(x => x.StockBatchId).HasDatabaseName("idx_stock_out_detail_batch_id");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_stock_out_detail_goods_id");

        builder.HasOne(x => x.StockOutOrder).WithMany(x => x.Details).HasForeignKey(x => x.StockOutOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SaleOrderDetail).WithMany().HasForeignKey(x => x.SaleOrderDetailId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockBatch).WithMany().HasForeignKey(x => x.StockBatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Goods).WithMany().HasForeignKey(x => x.GoodsId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.GoodsUnit).WithMany().HasForeignKey(x => x.GoodsUnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
