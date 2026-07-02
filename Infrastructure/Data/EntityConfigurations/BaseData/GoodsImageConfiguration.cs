using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class GoodsImageConfiguration : IEntityTypeConfiguration<GoodsImage>
{
    public void Configure(EntityTypeBuilder<GoodsImage> builder)
    {
        builder.ToTable("goods_image");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.Url).HasColumnName("url").HasMaxLength(500).IsRequired();
        builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(200);
        builder.Property(x => x.Sort).HasColumnName("sort");
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary");

        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_goods_image_goods_id");

        builder.HasOne(x => x.Goods)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

