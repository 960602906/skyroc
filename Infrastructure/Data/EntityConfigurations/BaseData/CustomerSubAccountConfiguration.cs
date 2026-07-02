using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class CustomerSubAccountConfiguration : IEntityTypeConfiguration<CustomerSubAccount>
{
    public void Configure(EntityTypeBuilder<CustomerSubAccount> builder)
    {
        builder.ToTable("customer_sub_account");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.CompanyId).HasColumnName("company_id");
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
        builder.Property(x => x.NickName).HasColumnName("nick_name").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(100);
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Username).IsUnique().HasDatabaseName("idx_customer_sub_account_username");
        builder.HasIndex(x => x.CompanyId).HasDatabaseName("idx_customer_sub_account_company_id");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("idx_customer_sub_account_customer_id");

        builder.HasOne(x => x.Company)
            .WithMany(x => x.SubAccounts)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.SubAccounts)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

