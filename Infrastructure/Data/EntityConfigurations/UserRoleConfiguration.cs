using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
///     UserRole 实体映射配置
/// </summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        // 表名
        builder.ToTable("sys_user_role", "public");

        // 主键（复合主键）
        builder.HasKey(x => new { x.UserId, x.RoleId });

        // UserId 配置
        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        // RoleId 配置
        builder.Property(x => x.RoleId)
            .HasColumnName("role_id")
            .HasColumnType("uuid")
            .IsRequired();

        // 外键约束
        builder.HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // 索引
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("idx_user_role_user_id");

        builder.HasIndex(x => x.RoleId)
            .HasDatabaseName("idx_user_role_role_id");
    }
}