using Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 个人 MCP 访问令牌元数据的 PostgreSQL 映射配置。
/// </summary>
public class McpAccessTokenConfiguration : IEntityTypeConfiguration<McpAccessToken>
{
    /// <summary>
    /// 配置令牌哈希、非敏感前缀、授权范围、有效期和用户隔离索引。
    /// </summary>
    /// <param name="builder">MCP 访问令牌实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<McpAccessToken> builder)
    {
        builder.ToTable("mcp_access_token", table =>
        {
            table.HasCheckConstraint("ck_mcp_access_token_scopes", "char_length(btrim(scopes)) > 0");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(32).IsRequired();
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(x => x.Scopes).HasColumnName("scopes").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RevokedTime).HasColumnName("revoked_time")
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.LastUsedTime).HasColumnName("last_used_time")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.Prefix).IsUnique().HasDatabaseName("idx_mcp_access_token_prefix");
        builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("idx_mcp_access_token_hash");
        builder.HasIndex(x => new { x.UserId, x.CreateTime, x.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("idx_mcp_access_token_user_create_time");
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt })
            .HasDatabaseName("idx_mcp_access_token_user_expires_at");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
