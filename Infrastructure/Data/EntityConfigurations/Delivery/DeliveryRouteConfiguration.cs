using Domain.Entities.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 配送路线实体的数据库映射配置。
/// </summary>
public class DeliveryRouteConfiguration : IEntityTypeConfiguration<DeliveryRoute>
{
    /// <summary>
    /// 配置配送路线表结构、字段约束和唯一编码索引。
    /// </summary>
    /// <param name="builder">配送路线实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<DeliveryRoute> builder)
    {
        builder.ToTable("delivery_route");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.Sort).HasColumnName("sort").IsRequired();
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_delivery_route_code");
        builder.HasIndex(x => x.Name).HasDatabaseName("idx_delivery_route_name");
    }
}
