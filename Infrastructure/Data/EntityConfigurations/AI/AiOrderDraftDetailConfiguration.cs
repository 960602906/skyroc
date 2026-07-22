using Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Constants;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// AI 订单草稿商品明细的 PostgreSQL 映射配置。
/// </summary>
public class AiOrderDraftDetailConfiguration : IEntityTypeConfiguration<AiOrderDraftDetail>
{
    /// <summary>
    /// 配置商品顺序、数量价格精度、价格来源快照和业务外键。
    /// </summary>
    /// <param name="builder">AI 订单草稿明细实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<AiOrderDraftDetail> builder)
    {
        builder.ToTable("ai_order_draft_detail", table =>
        {
            table.HasCheckConstraint("ck_ai_order_draft_detail_sort", "sort_order > 0");
            table.HasCheckConstraint(
                "ck_ai_order_draft_detail_quantities",
                "quantity > 0 AND base_quantity > 0 AND unit_conversion > 0");
            table.HasCheckConstraint(
                "ck_ai_order_draft_detail_prices",
                "fixed_price >= 0 AND (minimum_order_quantity_snapshot IS NULL OR minimum_order_quantity_snapshot > 0)");
            table.HasCheckConstraint("ck_ai_order_draft_detail_price_source", "price_source BETWEEN 1 AND 4");
            table.HasCheckConstraint(
                "ck_ai_order_draft_detail_source_record",
                "(price_source IN (2, 3) AND price_source_record_id IS NOT NULL) OR "
                + "(price_source IN (1, 4) AND price_source_record_id IS NULL)");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.AiOrderDraftId).HasColumnName("ai_order_draft_id").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(x => x.GoodsId).HasColumnName("goods_id").IsRequired();
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.GoodsUnitId).HasColumnName("goods_unit_id").IsRequired();
        builder.Property(x => x.GoodsUnitNameSnapshot).HasColumnName("goods_unit_name_snapshot")
            .HasMaxLength(100).IsRequired();
        builder.Property(x => x.Quantity).HasColumnName("quantity")
            .HasPrecision(18, NumericPrecision.QuantityScale).IsRequired();
        builder.Property(x => x.BaseQuantity).HasColumnName("base_quantity")
            .HasPrecision(18, NumericPrecision.QuantityScale).IsRequired();
        builder.Property(x => x.BaseUnitId).HasColumnName("base_unit_id");
        builder.Property(x => x.BaseUnitNameSnapshot).HasColumnName("base_unit_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.UnitConversion).HasColumnName("unit_conversion")
            .HasPrecision(18, NumericPrecision.QuantityScale).IsRequired();
        builder.Property(x => x.FixedPrice).HasColumnName("fixed_price")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.FixedGoodsUnitId).HasColumnName("fixed_goods_unit_id").IsRequired();
        builder.Property(x => x.FixedGoodsUnitNameSnapshot).HasColumnName("fixed_goods_unit_name_snapshot")
            .HasMaxLength(100).IsRequired();
        builder.Property(x => x.PriceSource).HasColumnName("price_source").HasColumnType("integer")
            .HasDefaultValue(AiOrderDraftPriceSource.Unresolved)
            .HasSentinel((AiOrderDraftPriceSource)0)
            .IsRequired();
        builder.Property(x => x.PriceSourceRecordId).HasColumnName("price_source_record_id");
        builder.Property(x => x.PriceSourceUpdatedTimeSnapshot).HasColumnName("price_source_updated_time_snapshot")
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.MinimumOrderQuantitySnapshot).HasColumnName("minimum_order_quantity_snapshot")
            .HasPrecision(18, NumericPrecision.QuantityScale);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => new { x.AiOrderDraftId, x.SortOrder })
            .IsUnique()
            .HasDatabaseName("idx_ai_order_draft_detail_draft_sort");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_ai_order_draft_detail_goods_id");
        builder.HasIndex(x => x.GoodsUnitId).HasDatabaseName("idx_ai_order_draft_detail_goods_unit_id");
        builder.HasIndex(x => x.BaseUnitId).HasDatabaseName("idx_ai_order_draft_detail_base_unit_id");
        builder.HasIndex(x => x.FixedGoodsUnitId).HasDatabaseName("idx_ai_order_draft_detail_fixed_unit_id");

        builder.HasOne(x => x.AiOrderDraft)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.AiOrderDraftId)
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
            .OnDelete(DeleteBehavior.Restrict);
    }
}
