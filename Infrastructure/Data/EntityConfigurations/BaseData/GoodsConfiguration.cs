using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class GoodsConfiguration : IEntityTypeConfiguration<Goods>
{
    public void Configure(EntityTypeBuilder<Goods> builder)
    {
        builder.ToTable("goods");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.GoodsTypeId).HasColumnName("goods_type_id");
        builder.Property(x => x.BaseUnitId).HasColumnName("base_unit_id");
        builder.Property(x => x.DefaultSupplierId).HasColumnName("default_supplier_id");
        builder.Property(x => x.DefaultWareId).HasColumnName("default_ware_id");
        builder.Property(x => x.Spec).HasColumnName("spec").HasMaxLength(100);
        builder.Property(x => x.Brand).HasColumnName("brand").HasMaxLength(100);
        builder.Property(x => x.Origin).HasColumnName("origin").HasMaxLength(100);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(x => x.TaxRate).HasColumnName("tax_rate").HasColumnType("numeric(8,4)");
        builder.Property(x => x.IsOnSale).HasColumnName("is_on_sale").HasDefaultValue(true);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_goods_code");
        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("idx_goods_name");
        builder.HasIndex(x => x.GoodsTypeId).HasDatabaseName("idx_goods_type_id");
        builder.HasIndex(x => x.DefaultSupplierId).HasDatabaseName("idx_goods_default_supplier_id");
        builder.HasIndex(x => x.DefaultWareId).HasDatabaseName("idx_goods_default_ware_id");

        builder.HasOne(x => x.GoodsType)
            .WithMany(x => x.Goods)
            .HasForeignKey(x => x.GoodsTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BaseUnit)
            .WithMany()
            .HasForeignKey(x => x.BaseUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DefaultSupplier)
            .WithMany()
            .HasForeignKey(x => x.DefaultSupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DefaultWare)
            .WithMany(x => x.DefaultGoods)
            .HasForeignKey(x => x.DefaultWareId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
