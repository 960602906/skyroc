using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;
/// <summary>
///  操作日志配置
/// </summary>
public class OperationLogConfiguration: IEntityTypeConfiguration<OperationLog>
{
    public void Configure(EntityTypeBuilder<OperationLog> builder)
    {
        builder.ToTable("sys_operation_log");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()") // PostgreSQL
            .ValueGeneratedOnAdd();
        builder.Property(x => x.Module)
            .HasColumnName("module")
            .IsRequired()
            .HasMaxLength(64);
        builder.Property(x => x.OperationType)
            .HasColumnName("operation_type")
            .IsRequired()
            .HasMaxLength(64);
        builder.Property(x => x.Desc)
            .HasColumnName("desc")
            .IsRequired()
            .HasMaxLength(512);
        builder.Property(x => x.Method)
            .HasColumnName("method")
            .IsRequired()
            .HasMaxLength(10);
        builder.Property(x => x.Url)
            .HasColumnName("url")
            .IsRequired()
            .HasMaxLength(512);
        builder.Property(x => x.RequestParams)
            .HasColumnName("request_params")
            .HasColumnType("text");
        builder.Property(x => x.ResponseResult)
            .HasColumnName("response_result")
            .HasColumnType("text");
        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(x => x.Location)
            .HasColumnName("location")
            .HasMaxLength(255);
        builder.Property(x => x.Browser)
            .HasColumnName("browser")
            .HasMaxLength(255);
        builder.Property(x => x.Os)
            .HasColumnName("os")
            .HasMaxLength(255);

        builder.Property(x => x.IsSuccess)
            .HasColumnName("is_success");
        
        builder.Property(x => x.ExecutionDuration)
            .HasColumnName("execution_duration");
        
        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message");
        
        
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
 
        // 索引
        builder.HasIndex(x => x.CreateBy)
            .HasDatabaseName("idx_operation_log_create_by");
        builder.HasIndex(x => x.CreateTime)
            .HasDatabaseName("idx_operation_log_create_time");
        builder.HasIndex(x => new { x.Module, x.OperationType })
            .HasDatabaseName("idx_operation_log_module_type");
        
        // ⭐ PostgreSQL 注释（可选）
        builder.ToTable(t => t.HasComment("操作日志表"));
    }
}