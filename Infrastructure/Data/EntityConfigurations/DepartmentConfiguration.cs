using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;
/// <summary>
///  部门配置
/// </summary>
public class DepartmentConfiguration:IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("sys_department");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()") // PostgreSQL
            .ValueGeneratedOnAdd();
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(64)
            .HasComment("部门名称");
        builder.Property(x => x.Code)
            .HasColumnName("code")
            .IsRequired()
            .HasMaxLength(64)
            .HasComment("部门代码");
        builder.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20)
            .HasComment("联系电话");
        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(100)
            .HasComment("邮箱");
        builder.Property(x => x.Remark)
            .HasColumnName("remark")
            .HasMaxLength(500)
            .HasComment("备注");

        builder.Property(x => x.Sort)
            .HasColumnName("sort");
        builder.Property(x => x.ParentId)
            .HasColumnName("parent_id");
        builder.Property(x => x.LeaderId)
            .HasColumnName("leader_id");
        
        // 公共字段
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
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("idx_department_code");
        
        // 外键：负责人
        builder.HasOne(x => x.Leader)
            .WithMany()
            .HasForeignKey(x => x.LeaderId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // 自引用：父级部门
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}