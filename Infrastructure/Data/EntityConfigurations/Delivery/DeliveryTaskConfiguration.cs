using Domain.Entities.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 配送任务实体的数据库映射配置。
/// </summary>
public class DeliveryTaskConfiguration : IEntityTypeConfiguration<DeliveryTask>
{
    /// <summary>
    /// 配置配送任务快照字段、状态、唯一来源约束、查询索引及业务外键。
    /// </summary>
    /// <param name="builder">配送任务实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<DeliveryTask> builder)
    {
        builder.ToTable("delivery_task");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.TaskNo).HasColumnName("task_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.StockOutOrderId).HasColumnName("stock_out_order_id").IsRequired();
        builder.Property(x => x.SaleOrderId).HasColumnName("sale_order_id").IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.CustomerNameSnapshot).HasColumnName("customer_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.ContactNameSnapshot).HasColumnName("contact_name_snapshot").HasMaxLength(50);
        builder.Property(x => x.ContactPhoneSnapshot).HasColumnName("contact_phone_snapshot").HasMaxLength(30);
        builder.Property(x => x.DeliveryAddressSnapshot).HasColumnName("delivery_address_snapshot").HasMaxLength(500);
        builder.Property(x => x.WareId).HasColumnName("ware_id").IsRequired();
        builder.Property(x => x.WareNameSnapshot).HasColumnName("ware_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.DriverId).HasColumnName("driver_id");
        builder.Property(x => x.DriverNameSnapshot).HasColumnName("driver_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.DriverPhoneSnapshot).HasColumnName("driver_phone_snapshot").HasMaxLength(30);
        builder.Property(x => x.CarrierId).HasColumnName("carrier_id");
        builder.Property(x => x.CarrierNameSnapshot).HasColumnName("carrier_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.RouteId).HasColumnName("route_id");
        builder.Property(x => x.RouteNameSnapshot).HasColumnName("route_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.RouteSequence).HasColumnName("route_sequence");
        builder.Property(x => x.DeliveryStatus).HasColumnName("delivery_status").HasColumnType("integer")
            .HasDefaultValue(DeliveryTaskStatus.PendingAssign).IsRequired();
        builder.Property(x => x.OutTime).HasColumnName("out_time").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.AssignedTime).HasColumnName("assigned_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.PlannedTime).HasColumnName("planned_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.TaskNo).IsUnique().HasDatabaseName("idx_delivery_task_no");
        builder.HasIndex(x => x.StockOutOrderId).IsUnique().HasDatabaseName("idx_delivery_task_stock_out_order_id");
        builder.HasIndex(x => new { x.DeliveryStatus, x.OutTime }).HasDatabaseName("idx_delivery_task_status_out_time");
        builder.HasIndex(x => new { x.DriverId, x.DeliveryStatus }).HasDatabaseName("idx_delivery_task_driver_status");
        builder.HasIndex(x => new { x.RouteId, x.RouteSequence }).HasDatabaseName("idx_delivery_task_route_sequence");

        builder.HasOne(x => x.StockOutOrder).WithMany().HasForeignKey(x => x.StockOutOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SaleOrder).WithMany().HasForeignKey(x => x.SaleOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Ware).WithMany().HasForeignKey(x => x.WareId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Carrier).WithMany().HasForeignKey(x => x.CarrierId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Route).WithMany().HasForeignKey(x => x.RouteId).OnDelete(DeleteBehavior.SetNull);
    }
}
