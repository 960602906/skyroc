using Domain.Entities.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 受保护上传文件元数据的数据库映射配置。
/// </summary>
public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    /// <summary>
    /// 配置不可公开的存储键、经验证 MIME 类型、文件大小约束和创建人查询索引。
    /// </summary>
    /// <param name="builder">文件元数据实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("stored_file", table =>
        {
            table.HasCheckConstraint("ck_stored_file_size", "file_size > 0 AND file_size <= 10485760");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.StorageKey).HasColumnName("storage_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.FileSize).HasColumnName("file_size").HasColumnType("bigint").IsRequired();
        builder.Property(x => x.CreateBy).IsRequired();

        builder.HasIndex(x => x.StorageKey).IsUnique().HasDatabaseName("uk_stored_file_storage_key");
        builder.HasIndex(x => new { x.CreateBy, x.CreateTime }).HasDatabaseName("idx_stored_file_creator_time");
    }
}
