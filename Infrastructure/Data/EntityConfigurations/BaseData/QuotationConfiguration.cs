using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("quotation");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(x => x.EffectiveStart).HasColumnName("effective_start").HasColumnType("timestamp with time zone");
        builder.Property(x => x.EffectiveEnd).HasColumnName("effective_end").HasColumnType("timestamp with time zone");
        builder.Property(x => x.IsAudited).HasColumnName("is_audited");

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_quotation_code");
        builder.HasIndex(x => x.Name).HasDatabaseName("idx_quotation_name");
    }
}

