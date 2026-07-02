using Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class SaleOrderDetailConfiguration : IEntityTypeConfiguration<SaleOrderDetail>
{
    public void Configure(EntityTypeBuilder<SaleOrderDetail> builder)
    {
        builder.ToTable("sale_order_detail");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.SaleOrderId).HasColumnName("sale_order_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.GoodsImageSnapshot).HasColumnName("goods_image_snapshot").HasMaxLength(500);
        builder.Property(x => x.GoodsTypeNameSnapshot).HasColumnName("goods_type_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.GoodsDescriptionSnapshot).HasColumnName("goods_description_snapshot").HasMaxLength(1000);
        builder.Property(x => x.GoodsUnitId).HasColumnName("goods_unit_id");
        builder.Property(x => x.GoodsUnitNameSnapshot).HasColumnName("goods_unit_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.BaseQuantity).HasColumnName("base_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.BaseUnitId).HasColumnName("base_unit_id");
        builder.Property(x => x.BaseUnitNameSnapshot).HasColumnName("base_unit_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.UnitConversion).HasColumnName("unit_conversion").HasColumnType("numeric(18,6)");
        builder.Property(x => x.FixedPrice).HasColumnName("fixed_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.FixedGoodsUnitId).HasColumnName("fixed_goods_unit_id");
        builder.Property(x => x.FixedGoodsUnitNameSnapshot).HasColumnName("fixed_goods_unit_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.TotalPrice).HasColumnName("total_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);
        builder.Property(x => x.InnerRemark).HasColumnName("inner_remark").HasMaxLength(500);
        builder.Property(x => x.CustomerCheckStatus).HasColumnName("customer_check_status").HasColumnType("integer").HasDefaultValue(OrderCustomerCheckStatus.Pending);
        builder.Property(x => x.CustomerCheckBaseQuantity).HasColumnName("customer_check_base_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.CustomerCheckPrice).HasColumnName("customer_check_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.HasPurchasePlan).HasColumnName("has_purchase_plan");

        builder.HasIndex(x => x.SaleOrderId).HasDatabaseName("idx_sale_order_detail_order_id");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_sale_order_detail_goods_id");
        builder.HasIndex(x => x.GoodsUnitId).HasDatabaseName("idx_sale_order_detail_unit_id");

        builder.HasOne(x => x.SaleOrder)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.SaleOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Goods)
            .WithMany()
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GoodsUnit)
            .WithMany()
            .HasForeignKey(x => x.GoodsUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BaseUnit)
            .WithMany()
            .HasForeignKey(x => x.BaseUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.FixedGoodsUnit)
            .WithMany()
            .HasForeignKey(x => x.FixedGoodsUnitId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
