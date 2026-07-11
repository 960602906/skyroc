using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>通知公告的 PostgreSQL 映射配置。</summary>
public class NoticeConfiguration : IEntityTypeConfiguration<Notice>
{
    /// <summary>配置公告正文、发布状态边界和列表查询索引。</summary>
    /// <param name="builder">通知公告实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<Notice> builder)
    {
        builder.ToTable("sys_notice", table => table.HasCheckConstraint("ck_sys_notice_status", "notice_status BETWEEN 0 AND 1"));
        builder.ConfigureBaseEntity();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        builder.Property(x => x.NoticeStatus).HasColumnName("notice_status").HasColumnType("integer").IsRequired();
        builder.Property(x => x.PublishedTime).HasColumnName("published_time").HasColumnType("timestamp with time zone");
        builder.HasIndex(x => new { x.NoticeStatus, x.PublishedTime }).HasDatabaseName("idx_sys_notice_status_published");
        builder.HasIndex(x => x.CreateTime).HasDatabaseName("idx_sys_notice_created");
    }
}
