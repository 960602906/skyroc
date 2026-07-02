using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class PurchaseRuleGoodsConfiguration : IEntityTypeConfiguration<PurchaseRuleGoods>
{
    public void Configure(EntityTypeBuilder<PurchaseRuleGoods> builder)
    {
        builder.ToTable("purchase_rule_goods");

        builder.HasKey(x => new { x.PurchaseRuleId, x.GoodsId });

        builder.Property(x => x.PurchaseRuleId).HasColumnName("purchase_rule_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id");

        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_purchase_rule_goods_goods_id");

        builder.HasOne(x => x.PurchaseRule)
            .WithMany(x => x.Goods)
            .HasForeignKey(x => x.PurchaseRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Goods)
            .WithMany(x => x.PurchaseRuleGoods)
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

