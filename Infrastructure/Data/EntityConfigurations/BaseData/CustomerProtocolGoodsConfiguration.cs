using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class CustomerProtocolGoodsConfiguration : IEntityTypeConfiguration<CustomerProtocolGoods>
{
    public void Configure(EntityTypeBuilder<CustomerProtocolGoods> builder)
    {
        builder.ToTable("customer_protocol_goods");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.CustomerProtocolId).HasColumnName("customer_protocol_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.GoodsUnitId).HasColumnName("goods_unit_id");
        builder.Property(x => x.ProtocolPrice).HasColumnName("protocol_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.MinOrderQuantity).HasColumnName("min_order_quantity").HasColumnType("numeric(18,4)");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.CustomerProtocolId).HasDatabaseName("idx_customer_protocol_goods_protocol_id");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_customer_protocol_goods_goods_id");
        builder.HasIndex(x => x.GoodsUnitId).HasDatabaseName("idx_customer_protocol_goods_unit_id");
        builder.HasIndex(x => new { x.CustomerProtocolId, x.GoodsId, x.GoodsUnitId })
            .IsUnique()
            .HasDatabaseName("idx_customer_protocol_goods_unique_goods_unit");

        builder.HasOne(x => x.CustomerProtocol)
            .WithMany(x => x.Goods)
            .HasForeignKey(x => x.CustomerProtocolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Goods)
            .WithMany(x => x.CustomerProtocolGoods)
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GoodsUnit)
            .WithMany()
            .HasForeignKey(x => x.GoodsUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

