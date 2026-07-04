using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations.Storage;

public class StocktakingDetailConfiguration : IEntityTypeConfiguration<StocktakingDetail>
{
    public void Configure(EntityTypeBuilder<StocktakingDetail> builder)
    {
        builder.ToTable(
            "stocktaking_detail",
            table =>
            {
                table.HasCheckConstraint("ck_stocktaking_detail_book_quantity", "book_quantity >= 0");
                table.HasCheckConstraint("ck_stocktaking_detail_actual_quantity", "actual_quantity >= 0");
                table.HasCheckConstraint("ck_stocktaking_detail_unit_cost", "unit_cost >= 0");
            });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.StocktakingOrderId).HasColumnName("stocktaking_order_id");
        builder.Property(x => x.StockBatchId).HasColumnName("stock_batch_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.BatchNoSnapshot).HasColumnName("batch_no_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.BaseUnitId).HasColumnName("base_unit_id");
        builder.Property(x => x.BaseUnitNameSnapshot).HasColumnName("base_unit_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.BookQuantity).HasColumnName("book_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.ActualQuantity).HasColumnName("actual_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.DifferenceQuantity).HasColumnName("difference_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.UnitCost).HasColumnName("unit_cost").HasColumnType("numeric(18,4)");
        builder.Property(x => x.DifferenceAmount).HasColumnName("difference_amount").HasColumnType("numeric(18,4)");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => new { x.StocktakingOrderId, x.StockBatchId })
            .IsUnique()
            .HasDatabaseName("idx_stocktaking_detail_order_batch");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_stocktaking_detail_goods_id");

        builder.HasOne(x => x.StocktakingOrder).WithMany(x => x.Details).HasForeignKey(x => x.StocktakingOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.StockBatch).WithMany().HasForeignKey(x => x.StockBatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Goods).WithMany().HasForeignKey(x => x.GoodsId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BaseUnit).WithMany().HasForeignKey(x => x.BaseUnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
