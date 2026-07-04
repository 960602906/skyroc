using Domain.Entities.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 客户路线关系实体的数据库映射配置。
/// </summary>
public class CustomerRouteConfiguration : IEntityTypeConfiguration<CustomerRoute>
{
    /// <summary>
    /// 配置客户路线关系表结构、客户与路线的唯一约束及外键删除行为。
    /// </summary>
    /// <param name="builder">客户路线关系实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<CustomerRoute> builder)
    {
        builder.ToTable("customer_route");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.RouteId).HasColumnName("route_id").IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.Sort).HasColumnName("sort").IsRequired();

        builder.HasIndex(x => new { x.RouteId, x.CustomerId })
            .IsUnique()
            .HasDatabaseName("idx_customer_route_route_customer");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("idx_customer_route_customer_id");

        builder.HasOne(x => x.Route)
            .WithMany(x => x.CustomerRoutes)
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
