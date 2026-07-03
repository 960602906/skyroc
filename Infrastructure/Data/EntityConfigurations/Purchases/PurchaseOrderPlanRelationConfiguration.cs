using Domain.Entities.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations.Purchases;

public class PurchaseOrderPlanRelationConfiguration : IEntityTypeConfiguration<PurchaseOrderPlanRelation>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderPlanRelation> builder)
    {
        builder.ToTable("purchase_order_plan_rel");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.PurchaseOrderDetailId).HasColumnName("purchase_order_detail_id");
        builder.Property(x => x.PurchasePlanDetailId).HasColumnName("purchase_plan_detail_id");
        builder.Property(x => x.AllocatedQuantity).HasColumnName("allocated_quantity").HasColumnType("numeric(18,6)");

        builder.HasIndex(x => new { x.PurchaseOrderDetailId, x.PurchasePlanDetailId })
            .IsUnique()
            .HasDatabaseName("idx_purchase_order_plan_rel_detail_plan");
        builder.HasIndex(x => x.PurchasePlanDetailId).HasDatabaseName("idx_purchase_order_plan_rel_plan_detail_id");

        builder.HasOne(x => x.PurchaseOrderDetail)
            .WithMany(x => x.PlanRelations)
            .HasForeignKey(x => x.PurchaseOrderDetailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PurchasePlanDetail)
            .WithMany(x => x.PurchaseOrderRelations)
            .HasForeignKey(x => x.PurchasePlanDetailId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
