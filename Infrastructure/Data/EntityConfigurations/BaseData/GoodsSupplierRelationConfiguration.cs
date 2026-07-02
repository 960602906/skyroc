using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class GoodsSupplierRelationConfiguration : IEntityTypeConfiguration<GoodsSupplierRelation>
{
    public void Configure(EntityTypeBuilder<GoodsSupplierRelation> builder)
    {
        builder.ToTable("goods_supplier_rel");

        builder.HasKey(x => new { x.GoodsId, x.SupplierId });

        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.IsDefault).HasColumnName("is_default");

        builder.HasIndex(x => x.SupplierId).HasDatabaseName("idx_goods_supplier_rel_supplier_id");

        builder.HasOne(x => x.Goods)
            .WithMany(x => x.SupplierRelations)
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.GoodsRelations)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

