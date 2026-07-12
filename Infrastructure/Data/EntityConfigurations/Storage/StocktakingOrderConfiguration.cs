using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations.Storage;

public class StocktakingOrderConfiguration : IEntityTypeConfiguration<StocktakingOrder>
{
    public void Configure(EntityTypeBuilder<StocktakingOrder> builder)
    {
        builder.ToTable("stocktaking_order");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.StocktakingNo).HasColumnName("stocktaking_no").HasMaxLength(50).IsRequired();
        // Draft=1，CLR 默认 0 不是合法状态；显式 sentinel 避免 EF 20601，并在值为 0 时使用库默认 Draft
        builder.Property(x => x.BusinessStatus)
            .HasColumnName("business_status")
            .HasColumnType("integer")
            .HasDefaultValue(StockDocumentStatus.Draft)
            .HasSentinel((StockDocumentStatus)0);
        builder.Property(x => x.WareId).HasColumnName("ware_id");
        builder.Property(x => x.WareNameSnapshot).HasColumnName("ware_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.StocktakingTime).HasColumnName("stocktaking_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.TotalBookQuantity).HasColumnName("total_book_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.TotalActualQuantity).HasColumnName("total_actual_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.TotalDifferenceQuantity).HasColumnName("total_difference_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.IsAdjustmentApplied).HasColumnName("is_adjustment_applied").HasDefaultValue(false);
        builder.Property(x => x.AdjustmentTime).HasColumnName("adjustment_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.AuditUserId).HasColumnName("audit_user_id");
        builder.Property(x => x.AuditUserNameSnapshot).HasColumnName("audit_user_name_snapshot").HasMaxLength(50);
        builder.Property(x => x.AuditTime).HasColumnName("audit_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReverseUserId).HasColumnName("reverse_user_id");
        builder.Property(x => x.ReverseUserNameSnapshot).HasColumnName("reverse_user_name_snapshot").HasMaxLength(50);
        builder.Property(x => x.ReverseTime).HasColumnName("reverse_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.StocktakingNo).IsUnique().HasDatabaseName("idx_stocktaking_order_no");
        builder.HasIndex(x => new { x.WareId, x.BusinessStatus, x.StocktakingTime }).HasDatabaseName("idx_stocktaking_order_ware_status_time");

        builder.HasOne(x => x.Ware)
            .WithMany()
            .HasForeignKey(x => x.WareId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
