using Domain.Entities.AfterSales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 售后取货任务的数据库映射配置。
/// </summary>
public class PickupTaskConfiguration : IEntityTypeConfiguration<PickupTask>
{
    /// <summary>
    /// 配置任务编号、联系人快照、履约时间、唯一来源约束和司机外键。
    /// </summary>
    /// <param name="builder">取货任务实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<PickupTask> builder)
    {
        builder.ToTable("pickup_task", table =>
        {
            table.HasCheckConstraint("ck_pickup_task_status", "pickup_status BETWEEN 1 AND 5");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.TaskNo).HasColumnName("task_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.AfterSaleId).HasColumnName("after_sale_id").IsRequired();
        builder.Property(x => x.AfterSaleGoodsId).HasColumnName("after_sale_goods_id").IsRequired();
        builder.Property(x => x.DriverId).HasColumnName("driver_id");
        builder.Property(x => x.DriverNameSnapshot).HasColumnName("driver_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.DriverPhoneSnapshot).HasColumnName("driver_phone_snapshot").HasMaxLength(30);
        builder.Property(x => x.ContactNameSnapshot).HasColumnName("contact_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.ContactPhoneSnapshot).HasColumnName("contact_phone_snapshot").HasMaxLength(30);
        builder.Property(x => x.PickupAddressSnapshot).HasColumnName("pickup_address_snapshot").HasMaxLength(500).IsRequired();
        builder.Property(x => x.PickupStatus).HasColumnName("pickup_status").HasColumnType("integer")
            .HasDefaultValue(PickupTaskStatus.PendingAssign).IsRequired();
        builder.Property(x => x.PlannedPickupTime).HasColumnName("planned_pickup_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.AssignedTime).HasColumnName("assigned_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.StartedTime).HasColumnName("started_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedTime).HasColumnName("completed_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.TaskNo).IsUnique().HasDatabaseName("idx_pickup_task_no");
        builder.HasIndex(x => x.AfterSaleGoodsId).IsUnique().HasDatabaseName("idx_pickup_task_after_sale_goods_id");
        builder.HasIndex(x => new { x.AfterSaleId, x.PickupStatus }).HasDatabaseName("idx_pickup_task_after_sale_status");
        builder.HasIndex(x => new { x.AfterSaleGoodsId, x.AfterSaleId }).HasDatabaseName("idx_pickup_task_goods_parent");
        builder.HasIndex(x => new { x.PickupStatus, x.PlannedPickupTime }).HasDatabaseName("idx_pickup_task_status_plan_time");
        builder.HasIndex(x => new { x.DriverId, x.PickupStatus }).HasDatabaseName("idx_pickup_task_driver_status");

        builder.HasOne(x => x.AfterSale)
            .WithMany(x => x.PickupTasks)
            .HasForeignKey(x => x.AfterSaleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AfterSaleGoods)
            .WithOne(x => x.PickupTask)
            .HasForeignKey<PickupTask>(x => new { x.AfterSaleGoodsId, x.AfterSaleId })
            .HasPrincipalKey<AfterSaleGoods>(x => new { x.Id, x.AfterSaleId })
            .HasConstraintName("fk_pickup_task_after_sale_goods")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
