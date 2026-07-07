using Domain.Entities.Traceability;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 商品溯源记录的数据库映射配置。
/// </summary>
public class TraceRecordConfiguration : IEntityTypeConfiguration<TraceRecord>
{
    /// <summary>
    /// 配置溯源编号与订单商品行唯一性、来源快照、查询索引和历史保护外键。
    /// </summary>
    /// <param name="builder">溯源记录实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<TraceRecord> builder)
    {
        builder.ToTable("trace_record");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.TraceNo).HasColumnName("trace_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SaleOrderId).HasColumnName("sale_order_id").IsRequired();
        builder.Property(x => x.SaleOrderNoSnapshot).HasColumnName("sale_order_no_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SaleOrderDetailId).HasColumnName("sale_order_detail_id").IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.CustomerNameSnapshot).HasColumnName("customer_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsId).HasColumnName("goods_id").IsRequired();
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.GoodsTypeNameSnapshot).HasColumnName("goods_type_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.StockInDetailId).HasColumnName("stock_in_detail_id");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierNameSnapshot).HasColumnName("supplier_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.WareId).HasColumnName("ware_id");
        builder.Property(x => x.WareNameSnapshot).HasColumnName("ware_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.BatchNoSnapshot).HasColumnName("batch_no_snapshot").HasMaxLength(100);
        builder.Property(x => x.InspectionReportId).HasColumnName("inspection_report_id");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.TraceNo).IsUnique().HasDatabaseName("idx_trace_record_no");
        builder.HasIndex(x => x.SaleOrderDetailId).IsUnique().HasDatabaseName("idx_trace_record_sale_order_detail_id");
        builder.HasIndex(x => x.SaleOrderId).HasDatabaseName("idx_trace_record_sale_order_id");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("idx_trace_record_customer_id");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_trace_record_goods_id");
        builder.HasIndex(x => x.StockInDetailId).HasDatabaseName("idx_trace_record_stock_in_detail_id");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("idx_trace_record_supplier_id");
        builder.HasIndex(x => x.WareId).HasDatabaseName("idx_trace_record_ware_id");
        builder.HasIndex(x => x.InspectionReportId).HasDatabaseName("idx_trace_record_inspection_report_id");

        builder.HasOne(x => x.SaleOrder)
            .WithMany()
            .HasForeignKey(x => x.SaleOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SaleOrderDetail)
            .WithMany()
            .HasForeignKey(x => x.SaleOrderDetailId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Goods)
            .WithMany()
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockInDetail)
            .WithMany()
            .HasForeignKey(x => x.StockInDetailId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Ware)
            .WithMany()
            .HasForeignKey(x => x.WareId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.InspectionReport)
            .WithMany()
            .HasForeignKey(x => x.InspectionReportId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
