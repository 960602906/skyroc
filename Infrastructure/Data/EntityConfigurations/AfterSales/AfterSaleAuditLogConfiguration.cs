using Domain.Entities.AfterSales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 售后审核记录的数据库映射配置。
/// </summary>
public class AfterSaleAuditLogConfiguration : IEntityTypeConfiguration<AfterSaleAuditLog>
{
    /// <summary>
    /// 配置审核动作、状态轨迹、操作人快照、时间索引和业务外键。
    /// </summary>
    /// <param name="builder">售后审核记录实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<AfterSaleAuditLog> builder)
    {
        builder.ToTable("after_sale_audit_log", table =>
        {
            table.HasCheckConstraint("ck_after_sale_audit_action", "action BETWEEN 1 AND 5");
            table.HasCheckConstraint(
                "ck_after_sale_audit_statuses",
                "previous_status BETWEEN 1 AND 5 AND current_status BETWEEN 1 AND 5");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.AfterSaleId).HasColumnName("after_sale_id").IsRequired();
        builder.Property(x => x.Action).HasColumnName("action").HasColumnType("integer").IsRequired();
        builder.Property(x => x.PreviousStatus).HasColumnName("previous_status").HasColumnType("integer").IsRequired();
        builder.Property(x => x.CurrentStatus).HasColumnName("current_status").HasColumnType("integer").IsRequired();
        builder.Property(x => x.AuditUserId).HasColumnName("audit_user_id");
        builder.Property(x => x.AuditUserNameSnapshot).HasColumnName("audit_user_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(x => x.AuditTime).HasColumnName("audit_time").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => new { x.AfterSaleId, x.AuditTime }).HasDatabaseName("idx_after_sale_audit_order_time");
        builder.HasIndex(x => x.AuditUserId).HasDatabaseName("idx_after_sale_audit_user_id");

        builder.HasOne(x => x.AfterSale)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.AfterSaleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.AuditUser)
            .WithMany()
            .HasForeignKey(x => x.AuditUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
