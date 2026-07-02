using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customer");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.CompanyId).HasColumnName("company_id");
        builder.Property(x => x.QuotationId).HasColumnName("quotation_id");
        builder.Property(x => x.DefaultWareId).HasColumnName("default_ware_id");
        builder.Property(x => x.UnifiedSocialCreditCode)
            .HasColumnName("unified_social_credit_code")
            .HasMaxLength(32);
        builder.Property(x => x.LegalRepresentative).HasColumnName("legal_representative").HasMaxLength(100);
        builder.Property(x => x.RegisteredCapital).HasColumnName("registered_capital").HasMaxLength(100);
        builder.Property(x => x.EstablishDate).HasColumnName("establish_date").HasColumnType("timestamp with time zone");
        builder.Property(x => x.BusinessTerm).HasColumnName("business_term").HasMaxLength(100);
        builder.Property(x => x.RegistrationStatus).HasColumnName("registration_status").HasMaxLength(50);
        builder.Property(x => x.RegistrationAuthority).HasColumnName("registration_authority").HasMaxLength(200);
        builder.Property(x => x.RegisteredAddress).HasColumnName("registered_address").HasMaxLength(500);
        builder.Property(x => x.BusinessScope).HasColumnName("business_scope").HasColumnType("text");
        builder.Property(x => x.InvoiceTitle).HasColumnName("invoice_title").HasMaxLength(200);
        builder.Property(x => x.TaxpayerIdentificationNumber)
            .HasColumnName("taxpayer_identification_number")
            .HasMaxLength(32);
        builder.Property(x => x.InvoiceAddress).HasColumnName("invoice_address").HasMaxLength(500);
        builder.Property(x => x.InvoicePhone).HasColumnName("invoice_phone").HasMaxLength(50);
        builder.Property(x => x.BankName).HasColumnName("bank_name").HasMaxLength(200);
        builder.Property(x => x.BankAccount).HasColumnName("bank_account").HasMaxLength(100);
        builder.Property(x => x.InvoiceReceiverName).HasColumnName("invoice_receiver_name").HasMaxLength(100);
        builder.Property(x => x.InvoiceReceiverPhone).HasColumnName("invoice_receiver_phone").HasMaxLength(50);
        builder.Property(x => x.InvoiceReceiverAddress).HasColumnName("invoice_receiver_address").HasMaxLength(500);
        builder.Property(x => x.InvoiceEmail).HasColumnName("invoice_email").HasMaxLength(100);
        builder.Property(x => x.ContactName).HasColumnName("contact_name").HasMaxLength(50);
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(20);
        builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(300);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_customer_code");
        builder.HasIndex(x => x.Name).HasDatabaseName("idx_customer_name");
        builder.HasIndex(x => x.CompanyId).HasDatabaseName("idx_customer_company_id");
        builder.HasIndex(x => x.QuotationId).HasDatabaseName("idx_customer_quotation_id");
        builder.HasIndex(x => x.DefaultWareId).HasDatabaseName("idx_customer_default_ware_id");
        builder.HasIndex(x => x.UnifiedSocialCreditCode).HasDatabaseName("idx_customer_uscc");
        builder.HasIndex(x => x.TaxpayerIdentificationNumber).HasDatabaseName("idx_customer_taxpayer_no");

        builder.HasOne(x => x.Company)
            .WithMany(x => x.Customers)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Quotation)
            .WithMany(x => x.DefaultCustomers)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DefaultWare)
            .WithMany(x => x.Customers)
            .HasForeignKey(x => x.DefaultWareId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

