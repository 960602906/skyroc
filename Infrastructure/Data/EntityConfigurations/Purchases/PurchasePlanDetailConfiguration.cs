using Domain.Entities.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations.Purchases;

public class PurchasePlanDetailConfiguration : IEntityTypeConfiguration<PurchasePlanDetail>
{
    public void Configure(EntityTypeBuilder<PurchasePlanDetail> builder)
    {
        builder.ToTable("purchase_plan_detail");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.PurchasePlanId).HasColumnName("purchase_plan_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.GoodsNameSnapshot).HasColumnName("goods_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.GoodsCodeSnapshot).HasColumnName("goods_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.PurchaseUnitId).HasColumnName("purchase_unit_id");
        builder.Property(x => x.PurchaseUnitNameSnapshot).HasColumnName("purchase_unit_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.RequiredQuantity).HasColumnName("required_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.PlannedQuantity).HasColumnName("planned_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.PurchasedQuantity).HasColumnName("purchased_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.PurchasePlanId).HasDatabaseName("idx_purchase_plan_detail_plan_id");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_purchase_plan_detail_goods_id");
        builder.HasIndex(x => x.PurchaseUnitId).HasDatabaseName("idx_purchase_plan_detail_unit_id");

        builder.HasOne(x => x.PurchasePlan)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.PurchasePlanId)
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
