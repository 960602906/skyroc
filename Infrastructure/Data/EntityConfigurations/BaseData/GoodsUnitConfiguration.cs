using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class GoodsUnitConfiguration : IEntityTypeConfiguration<GoodsUnit>
{
    public void Configure(EntityTypeBuilder<GoodsUnit> builder)
    {
        builder.ToTable("goods_unit");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
        builder.Property(x => x.ConversionRate)
            .HasColumnName("conversion_rate")
            .HasColumnType("numeric(18,6)")
            .HasDefaultValue(1m);
        builder.Property(x => x.IsBaseUnit).HasColumnName("is_base_unit");
        builder.Property(x => x.Sort).HasColumnName("sort");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_goods_unit_goods_id");
        builder.HasIndex(x => new { x.GoodsId, x.Name })
            .IsUnique()
            .HasDatabaseName("idx_goods_unit_goods_id_name");

        builder.HasOne(x => x.Goods)
            .WithMany(x => x.Units)
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

