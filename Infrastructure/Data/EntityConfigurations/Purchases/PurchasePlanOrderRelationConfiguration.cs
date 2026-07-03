using Domain.Entities.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations.Purchases;

public class PurchasePlanOrderRelationConfiguration : IEntityTypeConfiguration<PurchasePlanOrderRelation>
{
    public void Configure(EntityTypeBuilder<PurchasePlanOrderRelation> builder)
    {
        builder.ToTable("purchase_plan_order_rel");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.PurchasePlanDetailId).HasColumnName("purchase_plan_detail_id");
        builder.Property(x => x.SaleOrderId).HasColumnName("sale_order_id");
        builder.Property(x => x.SaleOrderDetailId).HasColumnName("sale_order_detail_id");
        builder.Property(x => x.RequiredQuantity).HasColumnName("required_quantity").HasColumnType("numeric(18,6)");

        builder.HasIndex(x => new { x.PurchasePlanDetailId, x.SaleOrderDetailId })
            .IsUnique()
            .HasDatabaseName("idx_purchase_plan_order_rel_detail_source");
        builder.HasIndex(x => x.SaleOrderId).HasDatabaseName("idx_purchase_plan_order_rel_order_id");
        builder.HasIndex(x => x.SaleOrderDetailId).HasDatabaseName("idx_purchase_plan_order_rel_order_detail_id");

        builder.HasOne(x => x.PurchasePlanDetail)
            .WithMany(x => x.OrderRelations)
            .HasForeignKey(x => x.PurchasePlanDetailId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SaleOrder)
            .WithMany()
            .HasForeignKey(x => x.SaleOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SaleOrderDetail)
            .WithMany()
            .HasForeignKey(x => x.SaleOrderDetailId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
