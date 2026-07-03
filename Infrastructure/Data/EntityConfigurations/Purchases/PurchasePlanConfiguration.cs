using Domain.Entities.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations.Purchases;

public class PurchasePlanConfiguration : IEntityTypeConfiguration<PurchasePlan>
{
    public void Configure(EntityTypeBuilder<PurchasePlan> builder)
    {
        builder.ToTable("purchase_plan");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.PlanNo).HasColumnName("plan_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.PlanDate).HasColumnName("plan_date").HasColumnType("timestamp with time zone");
        builder.Property(x => x.PurchasePattern).HasColumnName("purchase_pattern").HasColumnType("integer").HasDefaultValue(PurchasePattern.SupplierDirect);
        builder.Property(x => x.PurchaseStatus).HasColumnName("purchase_status").HasColumnType("integer").HasDefaultValue(PurchasePlanStatus.Unpublished);
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierNameSnapshot).HasColumnName("supplier_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.PurchaserId).HasColumnName("purchaser_id");
        builder.Property(x => x.PurchaserNameSnapshot).HasColumnName("purchaser_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.PlanNo).IsUnique().HasDatabaseName("idx_purchase_plan_plan_no");
        builder.HasIndex(x => new { x.PlanDate, x.PurchaseStatus }).HasDatabaseName("idx_purchase_plan_date_status");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("idx_purchase_plan_supplier_id");
        builder.HasIndex(x => x.PurchaserId).HasDatabaseName("idx_purchase_plan_purchaser_id");

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Purchaser)
            .WithMany()
            .HasForeignKey(x => x.PurchaserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
