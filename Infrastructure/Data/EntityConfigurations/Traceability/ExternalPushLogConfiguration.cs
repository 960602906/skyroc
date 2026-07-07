using Domain.Entities.Traceability;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 外部报送日志的数据库映射配置。
/// </summary>
public class ExternalPushLogConfiguration : IEntityTypeConfiguration<ExternalPushLog>
{
    /// <summary>
    /// 配置报送业务类型与状态约束、重试次数非负约束和按业务与状态检索的索引。
    /// </summary>
    /// <param name="builder">外部报送日志实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<ExternalPushLog> builder)
    {
        builder.ToTable("external_push_log", table =>
        {
            table.HasCheckConstraint("ck_external_push_log_business_type", "business_type BETWEEN 1 AND 3");
            table.HasCheckConstraint("ck_external_push_log_status", "push_status BETWEEN 1 AND 3");
            table.HasCheckConstraint("ck_external_push_log_retry", "retry_count >= 0");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.BusinessType).HasColumnName("business_type").HasColumnType("integer").IsRequired();
        builder.Property(x => x.BusinessId).HasColumnName("business_id").IsRequired();
        builder.Property(x => x.BusinessNoSnapshot).HasColumnName("business_no_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.PlatformCode).HasColumnName("platform_code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.PushStatus).HasColumnName("push_status").HasColumnType("integer")
            .HasDefaultValue(ExternalPushStatus.Pending).IsRequired();
        builder.Property(x => x.PushTime).HasColumnName("push_time").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ResponseTime).HasColumnName("response_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.RequestContent).HasColumnName("request_content").HasColumnType("text");
        builder.Property(x => x.ResponseContent).HasColumnName("response_content").HasColumnType("text");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(1000);
        builder.Property(x => x.RetryCount).HasColumnName("retry_count").HasDefaultValue(0).IsRequired();

        builder.HasIndex(x => new { x.BusinessType, x.BusinessId }).HasDatabaseName("idx_external_push_log_business");
        builder.HasIndex(x => new { x.PushStatus, x.PushTime }).HasDatabaseName("idx_external_push_log_status_time");
        builder.HasIndex(x => new { x.PlatformCode, x.PushTime }).HasDatabaseName("idx_external_push_log_platform_time");
    }
}
