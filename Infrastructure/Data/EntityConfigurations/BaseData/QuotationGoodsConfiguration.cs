using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class QuotationGoodsConfiguration : IEntityTypeConfiguration<QuotationGoods>
{
    public void Configure(EntityTypeBuilder<QuotationGoods> builder)
    {
        builder.ToTable("quotation_goods");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.QuotationId).HasColumnName("quotation_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.GoodsUnitId).HasColumnName("goods_unit_id");
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.MinOrderQuantity).HasColumnName("min_order_quantity").HasColumnType("numeric(18,4)");
        builder.Property(x => x.IsOnSale).HasColumnName("is_on_sale").HasDefaultValue(true);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.QuotationId).HasDatabaseName("idx_quotation_goods_quotation_id");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_quotation_goods_goods_id");
        builder.HasIndex(x => x.GoodsUnitId).HasDatabaseName("idx_quotation_goods_unit_id");
        builder.HasIndex(x => new { x.QuotationId, x.GoodsId, x.GoodsUnitId })
            .IsUnique()
            .HasDatabaseName("idx_quotation_goods_unique_goods_unit");

        builder.HasOne(x => x.Quotation)
            .WithMany(x => x.Goods)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Goods)
            .WithMany(x => x.QuotationGoods)
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GoodsUnit)
            .WithMany()
            .HasForeignKey(x => x.GoodsUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

