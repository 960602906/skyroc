using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>系统运营设置的 PostgreSQL 映射配置。</summary>
public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    /// <summary>配置稳定设置键的唯一约束和值 JSON 的文本存储。</summary>
    /// <param name="builder">系统设置实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("sys_setting", table => table.HasCheckConstraint("ck_sys_setting_key", "setting_key BETWEEN 1 AND 2"));
        builder.ConfigureBaseEntity();
        builder.Property(x => x.SettingKey).HasColumnName("setting_key").HasColumnType("integer").IsRequired();
        builder.Property(x => x.SettingValue).HasColumnName("setting_value").HasColumnType("text").IsRequired();
        builder.HasIndex(x => x.SettingKey).IsUnique().HasDatabaseName("uk_sys_setting_key");
    }
}
