using Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class OrderAuditLogConfiguration : IEntityTypeConfiguration<OrderAuditLog>
{
    public void Configure(EntityTypeBuilder<OrderAuditLog> builder)
    {
        builder.ToTable("order_audit_log");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.SaleOrderId).HasColumnName("sale_order_id");
        builder.Property(x => x.Action).HasColumnName("action").HasColumnType("integer");
        builder.Property(x => x.PreviousStatus).HasColumnName("previous_status").HasColumnType("integer");
        builder.Property(x => x.CurrentStatus).HasColumnName("current_status").HasColumnType("integer");
        builder.Property(x => x.AuditUserId).HasColumnName("audit_user_id");
        builder.Property(x => x.AuditUserNameSnapshot).HasColumnName("audit_user_name_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.AuditTime).HasColumnName("audit_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => new { x.SaleOrderId, x.AuditTime }).HasDatabaseName("idx_order_audit_log_order_time");
        builder.HasIndex(x => x.AuditUserId).HasDatabaseName("idx_order_audit_log_user_id");

        builder.HasOne(x => x.SaleOrder)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.SaleOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AuditUser)
            .WithMany()
            .HasForeignKey(x => x.AuditUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
