using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
///     Role 实体映射配置
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // 表名
        builder.ToTable("sys_role", "public");

        // 主键
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()") // PostgreSQL
            .ValueGeneratedOnAdd();

        // 属性配置
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.Desc)
            .HasColumnName("desc")
            .HasColumnType("varchar(200)");


        // 审计字段配置
        builder.Property(x => x.CreatedTime)
            .HasColumnName("created_time")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid");

        builder.Property(x => x.UpdateTime)
            .HasColumnName("update_time")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdateBy)
            .HasColumnName("update_by")
            .HasColumnType("uuid");
        
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("integer")
            .IsRequired();

        // 唯一索引
        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("idx_role_name");

        // 关系配置
        builder.HasMany(x => x.UserRoles)
            .WithOne(x => x.Role)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RoleMenus)
            .WithOne(x => x.Role)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}