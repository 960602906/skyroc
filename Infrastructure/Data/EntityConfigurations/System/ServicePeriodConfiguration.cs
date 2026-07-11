using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>运营服务时段的 PostgreSQL 映射配置。</summary>
public class ServicePeriodConfiguration : IEntityTypeConfiguration<ServicePeriod>
{
    /// <summary>配置日内时间边界、唯一名称、排序索引和持久化列。</summary>
    /// <param name="builder">服务时段实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<ServicePeriod> builder)
    {
        builder.ToTable("sys_service_period", table =>
        {
            table.HasCheckConstraint("ck_sys_service_period_time", "end_time > start_time");
            table.HasCheckConstraint("ck_sys_service_period_sort", "sort_order >= 0");
        });
        builder.ConfigureBaseEntity();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.StartTime).HasColumnName("start_time").HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.EndTime).HasColumnName("end_time").HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("uk_sys_service_period_name");
        builder.HasIndex(x => new { x.Status, x.SortOrder }).HasDatabaseName("idx_sys_service_period_status_sort");
    }
}
