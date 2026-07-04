using Domain.Entities.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 司机实体的数据库映射配置。
/// </summary>
public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    /// <summary>
    /// 配置司机表结构、字段约束、唯一编码索引及承运商外键关系。
    /// </summary>
    /// <param name="builder">司机实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("driver");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(x => x.CarrierId).HasColumnName("carrier_id");
        builder.Property(x => x.PlateNumber).HasColumnName("plate_number").HasMaxLength(20);
        builder.Property(x => x.LicenseNo).HasColumnName("license_no").HasMaxLength(50);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_driver_code");
        builder.HasIndex(x => x.CarrierId).HasDatabaseName("idx_driver_carrier_id");

        builder.HasOne(x => x.Carrier)
            .WithMany(x => x.Drivers)
            .HasForeignKey(x => x.CarrierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
