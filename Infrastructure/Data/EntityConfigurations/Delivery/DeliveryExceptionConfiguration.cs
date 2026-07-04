using Domain.Entities.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 配送异常实体的数据库映射配置。
/// </summary>
public class DeliveryExceptionConfiguration : IEntityTypeConfiguration<DeliveryException>
{
    /// <summary>
    /// 配置配送异常表结构、处理状态默认值、唯一编号索引及司机、客户外键关系。
    /// </summary>
    /// <param name="builder">配送异常实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<DeliveryException> builder)
    {
        builder.ToTable("delivery_exception");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.ExceptionNo).HasColumnName("exception_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.DeliveryTaskId).HasColumnName("delivery_task_id");
        builder.Property(x => x.DriverId).HasColumnName("driver_id");
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.HandleStatus)
            .HasColumnName("handle_status")
            .HasColumnType("integer")
            .HasDefaultValue(DeliveryExceptionStatus.Pending)
            .IsRequired();
        builder.Property(x => x.HandleRemark).HasColumnName("handle_remark").HasMaxLength(500);
        builder.Property(x => x.HandleTime)
            .HasColumnName("handle_time")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.ExceptionNo).IsUnique().HasDatabaseName("idx_delivery_exception_no");
        builder.HasIndex(x => x.DeliveryTaskId).HasDatabaseName("idx_delivery_exception_task_id");
        builder.HasIndex(x => x.DriverId).HasDatabaseName("idx_delivery_exception_driver_id");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("idx_delivery_exception_customer_id");

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DeliveryTask)
            .WithMany(x => x.Exceptions)
            .HasForeignKey(x => x.DeliveryTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
