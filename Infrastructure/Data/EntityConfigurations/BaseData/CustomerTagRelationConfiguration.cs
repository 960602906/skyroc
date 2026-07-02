using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class CustomerTagRelationConfiguration : IEntityTypeConfiguration<CustomerTagRelation>
{
    public void Configure(EntityTypeBuilder<CustomerTagRelation> builder)
    {
        builder.ToTable("customer_tag_rel");

        builder.HasKey(x => new { x.CustomerId, x.CustomerTagId });

        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.CustomerTagId).HasColumnName("customer_tag_id");

        builder.HasIndex(x => x.CustomerTagId).HasDatabaseName("idx_customer_tag_rel_tag_id");

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.TagRelations)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CustomerTag)
            .WithMany(x => x.CustomerRelations)
            .HasForeignKey(x => x.CustomerTagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

