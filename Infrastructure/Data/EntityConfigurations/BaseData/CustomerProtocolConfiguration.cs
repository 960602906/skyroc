using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class CustomerProtocolConfiguration : IEntityTypeConfiguration<CustomerProtocol>
{
    public void Configure(EntityTypeBuilder<CustomerProtocol> builder)
    {
        builder.ToTable("customer_protocol");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.QuotationId).HasColumnName("quotation_id");
        builder.Property(x => x.EffectiveStart).HasColumnName("effective_start").HasColumnType("timestamp with time zone");
        builder.Property(x => x.EffectiveEnd).HasColumnName("effective_end").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_customer_protocol_code");
        builder.HasIndex(x => x.QuotationId).HasDatabaseName("idx_customer_protocol_quotation_id");

        builder.HasOne(x => x.Quotation)
            .WithMany(x => x.CustomerProtocols)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

