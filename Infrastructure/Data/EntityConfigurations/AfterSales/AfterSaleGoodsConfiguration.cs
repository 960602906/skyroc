using Domain.Entities.AfterSales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Constants;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 售后商品明细的数据库映射配置。
/// </summary>
public class AfterSaleGoodsConfiguration : IEntityTypeConfiguration<AfterSaleGoods>
{
    /// <summary>
    /// 配置商品与单位快照、数量金额精度、来源唯一性和业务外键。
    /// </summary>
    /// <param name="builder">售后商品实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<AfterSaleGoods> builder)
    {
        builder.ToTable("after_sale_goods", table =>
        {
            table.HasCheckConstraint("ck_after_sale_goods_conversion_rate", "conversion_rate > 0");
            table.HasCheckConstraint(
                "ck_after_sale_goods_quantities",
                "actual_refund_quantity > 0 AND base_refund_quantity > 0");
            table.HasCheckConstraint(
                "ck_after_sale_goods_amounts",
                "unit_price >= 0 AND refund_amount >= 0");
            table.HasCheckConstraint("ck_after_sale_goods_type", "after_sale_type IN (1, 2)");
            table.HasCheckConstraint("ck_after_sale_goods_reason", "reason_type BETWEEN 1 AND 13");
            table.HasCheckConstraint("ck_after_sale_goods_handle", "handle_type BETWEEN 1 AND 6");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.AfterSaleId).HasColumnName("after_sale_id").IsRequired();
        builder.Property(x => x.SaleOrderDetailId).HasColumnName("sale_order_detail_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id").IsRequired();
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.GoodsTypeNameSnapshot).HasColumnName("goods_type_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.GoodsUnitId).HasColumnName("goods_unit_id").IsRequired();
        builder.Property(x => x.GoodsUnitNameSnapshot).HasColumnName("goods_unit_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.BaseUnitId).HasColumnName("base_unit_id");
        builder.Property(x => x.BaseUnitNameSnapshot).HasColumnName("base_unit_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.ConversionRate).HasColumnName("conversion_rate")
            .HasPrecision(18, NumericPrecision.QuantityScale).IsRequired();
        builder.Property(x => x.AfterSaleType).HasColumnName("after_sale_type").HasColumnType("integer").IsRequired();
        builder.Property(x => x.ActualRefundQuantity).HasColumnName("actual_refund_quantity")
            .HasPrecision(18, NumericPrecision.QuantityScale).IsRequired();
        builder.Property(x => x.BaseRefundQuantity).HasColumnName("base_refund_quantity")
            .HasPrecision(18, NumericPrecision.QuantityScale).IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.RefundAmount).HasColumnName("refund_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierNameSnapshot).HasColumnName("supplier_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.DepartmentId).HasColumnName("department_id");
        builder.Property(x => x.DepartmentNameSnapshot).HasColumnName("department_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.ReasonType).HasColumnName("reason_type").HasColumnType("integer").IsRequired();
        builder.Property(x => x.HandleType).HasColumnName("handle_type").HasColumnType("integer").IsRequired();
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => new { x.AfterSaleId, x.SaleOrderDetailId }).IsUnique()
            .HasDatabaseName("idx_after_sale_goods_order_detail");
        builder.HasAlternateKey(x => new { x.Id, x.AfterSaleId })
            .HasName("ak_after_sale_goods_id_after_sale_id");
        builder.HasIndex(x => x.SaleOrderDetailId).HasDatabaseName("idx_after_sale_goods_sale_order_detail_id");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_after_sale_goods_goods_id");
        builder.HasIndex(x => x.GoodsUnitId).HasDatabaseName("idx_after_sale_goods_unit_id");
        builder.HasIndex(x => x.BaseUnitId).HasDatabaseName("idx_after_sale_goods_base_unit_id");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("idx_after_sale_goods_supplier_id");
        builder.HasIndex(x => x.DepartmentId).HasDatabaseName("idx_after_sale_goods_department_id");

        builder.HasOne(x => x.AfterSale)
            .WithMany(x => x.Goods)
            .HasForeignKey(x => x.AfterSaleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SaleOrderDetail)
            .WithMany()
            .HasForeignKey(x => x.SaleOrderDetailId)
            .OnDelete(DeleteBehavior.Restrict);
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
        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
