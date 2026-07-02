using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class PurchaseRuleCustomerConfiguration : IEntityTypeConfiguration<PurchaseRuleCustomer>
{
    public void Configure(EntityTypeBuilder<PurchaseRuleCustomer> builder)
    {
        builder.ToTable("purchase_rule_customer");

        builder.HasKey(x => new { x.PurchaseRuleId, x.CustomerId });

        builder.Property(x => x.PurchaseRuleId).HasColumnName("purchase_rule_id");
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");

        builder.HasIndex(x => x.CustomerId).HasDatabaseName("idx_purchase_rule_customer_customer_id");

        builder.HasOne(x => x.PurchaseRule)
            .WithMany(x => x.Customers)
            .HasForeignKey(x => x.PurchaseRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.PurchaseRuleCustomers)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

