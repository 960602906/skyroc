using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class CustomerProtocolCustomerConfiguration : IEntityTypeConfiguration<CustomerProtocolCustomer>
{
    public void Configure(EntityTypeBuilder<CustomerProtocolCustomer> builder)
    {
        builder.ToTable("customer_protocol_customer");

        builder.HasKey(x => new { x.CustomerProtocolId, x.CustomerId });

        builder.Property(x => x.CustomerProtocolId).HasColumnName("customer_protocol_id");
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");

        builder.HasIndex(x => x.CustomerId).HasDatabaseName("idx_customer_protocol_customer_customer_id");

        builder.HasOne(x => x.CustomerProtocol)
            .WithMany(x => x.Customers)
            .HasForeignKey(x => x.CustomerProtocolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.CustomerProtocolCustomers)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

