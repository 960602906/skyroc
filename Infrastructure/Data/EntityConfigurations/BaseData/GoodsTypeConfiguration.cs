using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class GoodsTypeConfiguration : IEntityTypeConfiguration<GoodsType>
{
    public void Configure(EntityTypeBuilder<GoodsType> builder)
    {
        builder.ToTable("goods_type");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ParentId).HasColumnName("parent_id");
        builder.Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
        builder.Property(x => x.TaxCategoryCode).HasColumnName("tax_category_code").HasMaxLength(64);
        builder.Property(x => x.TaxCategoryName).HasColumnName("tax_category_name").HasMaxLength(200);
        builder.Property(x => x.InvoiceGoodsShortName).HasColumnName("invoice_goods_short_name").HasMaxLength(100);
        builder.Property(x => x.DefaultTaxRate).HasColumnName("default_tax_rate").HasColumnType("numeric(8,4)");
        builder.Property(x => x.IsTaxExempt).HasColumnName("is_tax_exempt").HasDefaultValue(false);
        builder.Property(x => x.TaxPolicyBasis).HasColumnName("tax_policy_basis").HasMaxLength(500);
        builder.Property(x => x.Sort).HasColumnName("sort");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_goods_type_code");
        builder.HasIndex(x => x.ParentId).HasDatabaseName("idx_goods_type_parent_id");
        builder.HasIndex(x => x.TaxCategoryCode).HasDatabaseName("idx_goods_type_tax_category_code");

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

