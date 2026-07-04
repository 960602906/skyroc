using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations.Storage;

public class StockBatchConfiguration : IEntityTypeConfiguration<StockBatch>
{
    public void Configure(EntityTypeBuilder<StockBatch> builder)
    {
        builder.ToTable(
            "stock_batch",
            table =>
            {
                table.HasCheckConstraint("ck_stock_batch_current_quantity", "current_quantity >= 0");
                table.HasCheckConstraint("ck_stock_batch_available_quantity", "available_quantity >= 0 AND available_quantity <= current_quantity");
                table.HasCheckConstraint("ck_stock_batch_unit_cost", "unit_cost >= 0");
            });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.WareId).HasColumnName("ware_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.BatchNo).HasColumnName("batch_no").HasMaxLength(100).IsRequired();
        builder.Property(x => x.BaseUnitId).HasColumnName("base_unit_id");
        builder.Property(x => x.BaseUnitNameSnapshot).HasColumnName("base_unit_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.CurrentQuantity).HasColumnName("current_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.AvailableQuantity).HasColumnName("available_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.UnitCost).HasColumnName("unit_cost").HasColumnType("numeric(18,4)");
        builder.Property(x => x.ProductDate).HasColumnName("product_date").HasColumnType("date");
        builder.Property(x => x.ExpireDate).HasColumnName("expire_date").HasColumnType("date");
        builder.Property(x => x.LastMovementTime).HasColumnName("last_movement_time").HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.WareId, x.GoodsId, x.BatchNo })
            .IsUnique()
            .HasDatabaseName("idx_stock_batch_ware_goods_batch");
        builder.HasIndex(x => new { x.GoodsId, x.ExpireDate }).HasDatabaseName("idx_stock_batch_goods_expire");

        builder.HasOne(x => x.Ware)
            .WithMany()
            .HasForeignKey(x => x.WareId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Goods)
            .WithMany()
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BaseUnit)
            .WithMany()
            .HasForeignKey(x => x.BaseUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
