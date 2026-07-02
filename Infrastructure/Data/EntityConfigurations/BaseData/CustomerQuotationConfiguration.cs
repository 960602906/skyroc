using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class CustomerQuotationConfiguration : IEntityTypeConfiguration<CustomerQuotation>
{
    public void Configure(EntityTypeBuilder<CustomerQuotation> builder)
    {
        builder.ToTable("customer_quotation");

        builder.HasKey(x => new { x.CustomerId, x.QuotationId });

        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.QuotationId).HasColumnName("quotation_id");
        builder.Property(x => x.IsDefault).HasColumnName("is_default");
        builder.Property(x => x.EffectiveStart).HasColumnName("effective_start").HasColumnType("timestamp with time zone");
        builder.Property(x => x.EffectiveEnd).HasColumnName("effective_end").HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.QuotationId).HasDatabaseName("idx_customer_quotation_quotation_id");

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.CustomerQuotations)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Quotation)
            .WithMany(x => x.CustomerQuotations)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

