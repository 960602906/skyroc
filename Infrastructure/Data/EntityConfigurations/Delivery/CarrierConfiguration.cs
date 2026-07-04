using Domain.Entities.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 承运商实体的数据库映射配置。
/// </summary>
public class CarrierConfiguration : IEntityTypeConfiguration<Carrier>
{
    /// <summary>
    /// 配置承运商表结构、字段约束和唯一编码索引。
    /// </summary>
    /// <param name="builder">承运商实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<Carrier> builder)
    {
        builder.ToTable("carrier");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ContactName).HasColumnName("contact_name").HasMaxLength(50);
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(20);
        builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(300);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_carrier_code");
        builder.HasIndex(x => x.Name).HasDatabaseName("idx_carrier_name");
    }
}
