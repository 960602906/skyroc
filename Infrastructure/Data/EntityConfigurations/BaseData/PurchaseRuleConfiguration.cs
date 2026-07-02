using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class PurchaseRuleConfiguration : IEntityTypeConfiguration<PurchaseRule>
{
    public void Configure(EntityTypeBuilder<PurchaseRule> builder)
    {
        builder.ToTable("purchase_rule");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.PurchaserId).HasColumnName("purchaser_id");
        builder.Property(x => x.WareId).HasColumnName("ware_id");
        builder.Property(x => x.GoodsTypeId).HasColumnName("goods_type_id");
        builder.Property(x => x.PurchasePattern).HasColumnName("purchase_pattern").HasDefaultValue(1);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_purchase_rule_code");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("idx_purchase_rule_supplier_id");
        builder.HasIndex(x => x.PurchaserId).HasDatabaseName("idx_purchase_rule_purchaser_id");
        builder.HasIndex(x => x.WareId).HasDatabaseName("idx_purchase_rule_ware_id");
        builder.HasIndex(x => x.GoodsTypeId).HasDatabaseName("idx_purchase_rule_goods_type_id");

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.PurchaseRules)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Purchaser)
            .WithMany(x => x.PurchaseRules)
            .HasForeignKey(x => x.PurchaserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Ware)
            .WithMany(x => x.PurchaseRules)
            .HasForeignKey(x => x.WareId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.GoodsType)
            .WithMany()
            .HasForeignKey(x => x.GoodsTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

