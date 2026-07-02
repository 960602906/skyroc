using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("supplier");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ContactName).HasColumnName("contact_name").HasMaxLength(50);
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(20);
        builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(300);
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(100);
        builder.Property(x => x.BankAccount).HasColumnName("bank_account").HasMaxLength(100);
        builder.Property(x => x.TaxNo).HasColumnName("tax_no").HasMaxLength(100);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_supplier_code");
        builder.HasIndex(x => x.Name).HasDatabaseName("idx_supplier_name");
    }
}

