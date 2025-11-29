using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
///     User 实体映射配置
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // 表名
        builder.ToTable("sys_user", "public");

        // 主键
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()") // PostgreSQL
            .ValueGeneratedOnAdd();

        // 属性配置
        builder.Property(x => x.Username)
            .HasColumnName("username")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.Gender)
            .HasColumnName("gender")
            .HasColumnType("integer")
            .IsRequired();


        builder.Property(x => x.NickName)
            .HasColumnName("nick_name")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasColumnType("varchar(20)");

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("varchar(255)")
            .IsRequired();

        // 审计字段
        builder.Property(x => x.CreateTime)
            .HasColumnName("create_time")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreateBy)
            .HasColumnName("create_by")
            .HasColumnType("uuid");
        
        builder.Property(x => x.CreateName)
            .HasColumnName("create_name")
            .HasColumnType("varchar(50)");

        builder.Property(x => x.UpdateTime)
            .HasColumnName("update_time")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdateBy)
            .HasColumnName("update_by")
            .HasColumnType("uuid");
        
        builder.Property(x => x.UpdateName)
            .HasColumnName("update_name")
            .HasColumnType("varchar(50)");
        
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("integer")
            .IsRequired();

        // 唯一索引
        builder.HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName("idx_user_username");
        
        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("idx_user_email");

        // 关系配置
        builder.HasMany(x => x.UserRoles)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}