using Domain.Entities.Traceability;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Constants;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 检测报告商品明细的数据库映射配置。
/// </summary>
public class InspectionReportGoodsConfiguration : IEntityTypeConfiguration<InspectionReportGoods>
{
    /// <summary>
    /// 配置商品与单位快照、送检数量精度、单品结论约束、来源唯一性和业务外键。
    /// </summary>
    /// <param name="builder">检测报告商品实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<InspectionReportGoods> builder)
    {
        builder.ToTable("inspection_report_goods", table =>
        {
            table.HasCheckConstraint("ck_inspection_report_goods_quantity", "sample_quantity > 0");
            table.HasCheckConstraint("ck_inspection_report_goods_conclusion", "conclusion BETWEEN 1 AND 3");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.InspectionReportId).HasColumnName("inspection_report_id").IsRequired();
        builder.Property(x => x.StockInDetailId).HasColumnName("stock_in_detail_id").IsRequired();
        builder.Property(x => x.GoodsId).HasColumnName("goods_id").IsRequired();
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.GoodsTypeNameSnapshot).HasColumnName("goods_type_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.GoodsUnitId).HasColumnName("goods_unit_id").IsRequired();
        builder.Property(x => x.GoodsUnitNameSnapshot).HasColumnName("goods_unit_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.SampleQuantity).HasColumnName("sample_quantity")
            .HasPrecision(18, NumericPrecision.QuantityScale).IsRequired();
        builder.Property(x => x.BatchNoSnapshot).HasColumnName("batch_no_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Conclusion).HasColumnName("conclusion").HasColumnType("integer")
            .HasDefaultValue(InspectionConclusion.Pending).IsRequired();
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => new { x.InspectionReportId, x.StockInDetailId }).IsUnique()
            .HasDatabaseName("idx_inspection_report_goods_source");
        builder.HasIndex(x => x.StockInDetailId).HasDatabaseName("idx_inspection_report_goods_stock_in_detail_id");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_inspection_report_goods_goods_id");
        builder.HasIndex(x => x.GoodsUnitId).HasDatabaseName("idx_inspection_report_goods_unit_id");

        builder.HasOne(x => x.InspectionReport)
            .WithMany(x => x.Goods)
            .HasForeignKey(x => x.InspectionReportId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.StockInDetail)
            .WithMany()
            .HasForeignKey(x => x.StockInDetailId)
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
