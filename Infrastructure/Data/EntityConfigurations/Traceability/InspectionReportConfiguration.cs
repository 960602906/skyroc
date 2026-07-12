using Domain.Entities.Traceability;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 检测报告主单的数据库映射配置。
/// </summary>
public class InspectionReportConfiguration : IEntityTypeConfiguration<InspectionReport>
{
    /// <summary>
    /// 配置报告编号唯一性、入库来源快照、检测结论约束、查询索引和业务外键。
    /// </summary>
    /// <param name="builder">检测报告实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<InspectionReport> builder)
    {
        builder.ToTable("inspection_report", table =>
        {
            table.HasCheckConstraint("ck_inspection_report_conclusion", "conclusion BETWEEN 1 AND 3");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.InspectionNo).HasColumnName("inspection_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.StockInOrderId).HasColumnName("stock_in_order_id").IsRequired();
        builder.Property(x => x.InNoSnapshot).HasColumnName("in_no_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.WareId).HasColumnName("ware_id").IsRequired();
        builder.Property(x => x.WareNameSnapshot).HasColumnName("ware_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierNameSnapshot).HasColumnName("supplier_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.InspectionOrg).HasColumnName("inspection_org").HasMaxLength(150).IsRequired();
        builder.Property(x => x.SampleTime).HasColumnName("sample_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.InspectTime).HasColumnName("inspect_time").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Conclusion).HasColumnName("conclusion").HasColumnType("integer")
            .HasDefaultValue(InspectionConclusion.Pending)
            .HasSentinel((InspectionConclusion)0)
            .IsRequired();
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.InspectionNo).IsUnique().HasDatabaseName("idx_inspection_report_no");
        builder.HasIndex(x => x.StockInOrderId).HasDatabaseName("idx_inspection_report_stock_in_order_id");
        builder.HasIndex(x => x.WareId).HasDatabaseName("idx_inspection_report_ware_id");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("idx_inspection_report_supplier_id");
        builder.HasIndex(x => new { x.Conclusion, x.InspectTime }).HasDatabaseName("idx_inspection_report_conclusion_time");

        builder.HasOne(x => x.StockInOrder)
            .WithMany()
            .HasForeignKey(x => x.StockInOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Ware)
            .WithMany()
            .HasForeignKey(x => x.WareId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
