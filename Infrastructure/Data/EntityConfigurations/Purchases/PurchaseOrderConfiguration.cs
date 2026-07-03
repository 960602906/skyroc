using Domain.Entities.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations.Purchases;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_order");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.PurchaseNo).HasColumnName("purchase_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierNameSnapshot).HasColumnName("supplier_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.PurchaserId).HasColumnName("purchaser_id");
        builder.Property(x => x.PurchaserNameSnapshot).HasColumnName("purchaser_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.PurchasePattern).HasColumnName("purchase_pattern").HasColumnType("integer").HasDefaultValue(PurchasePattern.SupplierDirect);
        builder.Property(x => x.ReceiveTime).HasColumnName("receive_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.BusinessStatus).HasColumnName("business_status").HasColumnType("integer").HasDefaultValue(PurchaseOrderStatus.Draft);
        builder.Property(x => x.SupplierContactNameSnapshot).HasColumnName("supplier_contact_name_snapshot").HasMaxLength(50);
        builder.Property(x => x.SupplierContactPhoneSnapshot).HasColumnName("supplier_contact_phone_snapshot").HasMaxLength(20);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.PurchaseNo).IsUnique().HasDatabaseName("idx_purchase_order_purchase_no");
        builder.HasIndex(x => new { x.ReceiveTime, x.BusinessStatus }).HasDatabaseName("idx_purchase_order_receive_status");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("idx_purchase_order_supplier_id");
        builder.HasIndex(x => x.PurchaserId).HasDatabaseName("idx_purchase_order_purchaser_id");

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
