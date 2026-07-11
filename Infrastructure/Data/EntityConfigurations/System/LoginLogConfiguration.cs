using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>登录日志的 PostgreSQL 映射配置。</summary>
public class LoginLogConfiguration : IEntityTypeConfiguration<LoginLog>
{
    /// <summary>配置登录来源、结果和审计时间的不可敏感持久化字段。</summary>
    /// <param name="builder">登录日志实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<LoginLog> builder)
    {
        builder.ToTable("sys_login_log");
        builder.ConfigureBaseEntity();
        builder.Property(x => x.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.Property(x => x.IsSuccess).HasColumnName("is_success").IsRequired();
        builder.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(500);
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(50).IsRequired();
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(x => x.LoginTime).HasColumnName("login_time").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => x.LoginTime).HasDatabaseName("idx_sys_login_log_time");
        builder.HasIndex(x => new { x.Username, x.LoginTime }).HasDatabaseName("idx_sys_login_log_username_time");
        builder.HasIndex(x => new { x.IsSuccess, x.LoginTime }).HasDatabaseName("idx_sys_login_log_success_time");
    }
}
