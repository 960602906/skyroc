using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
///     RoleMenu 实体映射配置
/// </summary>
public class RoleMenuConfiguration : IEntityTypeConfiguration<RoleMenu>
{
    public void Configure(EntityTypeBuilder<RoleMenu> builder)
    {
        // 表名
        builder.ToTable("sys_role_menu", "public");

        // 主键（复合主键）
        builder.HasKey(x => new { x.RoleId, x.MenuId });

        // RoleId 配置
        builder.Property(x => x.RoleId)
            .HasColumnName("role_id")
            .HasColumnType("uuid")
            .IsRequired();

        // MenuId 配置
        builder.Property(x => x.MenuId)
            .HasColumnName("menu_id")
            .HasColumnType("uuid")
            .IsRequired();


        // 外键约束
        builder.HasOne(x => x.Role)
            .WithMany(x => x.RoleMenus)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Menu)
            .WithMany(x => x.RoleMenus)
            .HasForeignKey(x => x.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        // 索引
        builder.HasIndex(x => x.RoleId)
            .HasDatabaseName("idx_role_menu_role_id");

        builder.HasIndex(x => x.MenuId)
            .HasDatabaseName("idx_role_menu_menu_id");
    }
}