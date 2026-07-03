using Domain.Entities.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations.Purchases;

public class PurchaseOrderDetailConfiguration : IEntityTypeConfiguration<PurchaseOrderDetail>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderDetail> builder)
    {
        builder.ToTable("purchase_order_detail");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.PurchaseOrderId).HasColumnName("purchase_order_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.GoodsInfoSnapshot).HasColumnName("goods_info_snapshot").HasMaxLength(2000);
        builder.Property(x => x.PurchaseUnitId).HasColumnName("purchase_unit_id");
        builder.Property(x => x.PurchaseUnitNameSnapshot).HasColumnName("purchase_unit_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.RequiredQuantity).HasColumnName("required_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.PurchaseQuantity).HasColumnName("purchase_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.PurchasePrice).HasColumnName("purchase_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.PurchaseTotalPrice).HasColumnName("purchase_total_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.ProductDate).HasColumnName("product_date").HasColumnType("date");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.PurchaseOrderId).HasDatabaseName("idx_purchase_order_detail_order_id");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_purchase_order_detail_goods_id");
        builder.HasIndex(x => x.PurchaseUnitId).HasDatabaseName("idx_purchase_order_detail_unit_id");

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Goods)
            .WithMany()
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PurchaseUnit)
            .WithMany()
            .HasForeignKey(x => x.PurchaseUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
