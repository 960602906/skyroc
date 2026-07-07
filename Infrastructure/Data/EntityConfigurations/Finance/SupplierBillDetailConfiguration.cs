using Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Constants;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 供应商待结单据明细的数据库映射配置。
/// </summary>
public class SupplierBillDetailConfiguration : IEntityTypeConfiguration<SupplierBillDetail>
{
    /// <summary>
    /// 配置来源单据、商品单位快照、数量金额精度、来源唯一约束和业务外键。
    /// </summary>
    /// <param name="builder">供应商待结单据明细实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<SupplierBillDetail> builder)
    {
        builder.ToTable("supplier_bill_detail", table =>
        {
            table.HasCheckConstraint("ck_supplier_bill_detail_source_type", "source_type IN (1, 2)");
            table.HasCheckConstraint("ck_supplier_bill_detail_conversion_rate", "conversion_rate > 0");
            table.HasCheckConstraint("ck_supplier_bill_detail_unit_price", "unit_price >= 0");
            table.HasCheckConstraint(
                "ck_supplier_bill_detail_source_amount",
                "(source_type = 1 AND quantity >= 0 AND base_quantity >= 0 AND amount >= 0) OR (source_type = 2 AND quantity <= 0 AND base_quantity <= 0 AND amount <= 0)");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.SupplierBillId).HasColumnName("supplier_bill_id").IsRequired();
        builder.Property(x => x.SourceType).HasColumnName("source_type").HasColumnType("integer").IsRequired();
        builder.Property(x => x.SourceDocumentId).HasColumnName("source_document_id").IsRequired();
        builder.Property(x => x.SourceDetailId).HasColumnName("source_detail_id").IsRequired();
        builder.Property(x => x.StockInOrderId).HasColumnName("stock_in_order_id");
        builder.Property(x => x.StockInDetailId).HasColumnName("stock_in_detail_id");
        builder.Property(x => x.StockOutOrderId).HasColumnName("stock_out_order_id");
        builder.Property(x => x.StockOutDetailId).HasColumnName("stock_out_detail_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id").IsRequired();
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.GoodsTypeNameSnapshot).HasColumnName("goods_type_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.GoodsUnitId).HasColumnName("goods_unit_id").IsRequired();
        builder.Property(x => x.GoodsUnitNameSnapshot).HasColumnName("goods_unit_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.BaseUnitId).HasColumnName("base_unit_id");
        builder.Property(x => x.BaseUnitNameSnapshot).HasColumnName("base_unit_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.Quantity).HasColumnName("quantity")
            .HasPrecision(18, NumericPrecision.QuantityScale).IsRequired();
        builder.Property(x => x.BaseQuantity).HasColumnName("base_quantity")
            .HasPrecision(18, NumericPrecision.QuantityScale).IsRequired();
        builder.Property(x => x.ConversionRate).HasColumnName("conversion_rate")
            .HasPrecision(18, NumericPrecision.QuantityScale).IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.BusinessTime).HasColumnName("business_time").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => new { x.SourceType, x.SourceDetailId })
            .IsUnique()
            .HasDatabaseName("idx_supplier_bill_detail_source_detail");
        builder.HasIndex(x => x.SupplierBillId).HasDatabaseName("idx_supplier_bill_detail_bill_id");
        builder.HasIndex(x => x.StockInDetailId).HasDatabaseName("idx_supplier_bill_detail_stock_in_detail_id");
        builder.HasIndex(x => x.StockOutDetailId).HasDatabaseName("idx_supplier_bill_detail_stock_out_detail_id");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_supplier_bill_detail_goods_id");
        builder.HasIndex(x => x.GoodsUnitId).HasDatabaseName("idx_supplier_bill_detail_unit_id");
        builder.HasIndex(x => x.BaseUnitId).HasDatabaseName("idx_supplier_bill_detail_base_unit_id");

        builder.HasOne(x => x.SupplierBill)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.SupplierBillId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.StockInOrder)
            .WithMany()
            .HasForeignKey(x => x.StockInOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockInDetail)
            .WithMany()
            .HasForeignKey(x => x.StockInDetailId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockOutOrder)
            .WithMany()
            .HasForeignKey(x => x.StockOutOrderId)
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
        builder.HasOne(x => x.BaseUnit)
            .WithMany()
            .HasForeignKey(x => x.BaseUnitId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
