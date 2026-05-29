using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class GoodsTypeConfiguration : IEntityTypeConfiguration<GoodsType>
{
    public void Configure(EntityTypeBuilder<GoodsType> builder)
    {
        builder.ToTable("goods_type");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ParentId).HasColumnName("parent_id");
        builder.Property(x => x.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
        builder.Property(x => x.TaxCategoryCode).HasColumnName("tax_category_code").HasMaxLength(64);
        builder.Property(x => x.TaxCategoryName).HasColumnName("tax_category_name").HasMaxLength(200);
        builder.Property(x => x.InvoiceGoodsShortName).HasColumnName("invoice_goods_short_name").HasMaxLength(100);
        builder.Property(x => x.DefaultTaxRate).HasColumnName("default_tax_rate").HasColumnType("numeric(8,4)");
        builder.Property(x => x.IsTaxExempt).HasColumnName("is_tax_exempt").HasDefaultValue(false);
        builder.Property(x => x.TaxPolicyBasis).HasColumnName("tax_policy_basis").HasMaxLength(500);
        builder.Property(x => x.Sort).HasColumnName("sort");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_goods_type_code");
        builder.HasIndex(x => x.ParentId).HasDatabaseName("idx_goods_type_parent_id");
        builder.HasIndex(x => x.TaxCategoryCode).HasDatabaseName("idx_goods_type_tax_category_code");

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class GoodsConfiguration : IEntityTypeConfiguration<Goods>
{
    public void Configure(EntityTypeBuilder<Goods> builder)
    {
        builder.ToTable("goods");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.GoodsTypeId).HasColumnName("goods_type_id");
        builder.Property(x => x.BaseUnitId).HasColumnName("base_unit_id");
        builder.Property(x => x.DefaultSupplierId).HasColumnName("default_supplier_id");
        builder.Property(x => x.DefaultWareId).HasColumnName("default_ware_id");
        builder.Property(x => x.Spec).HasColumnName("spec").HasMaxLength(100);
        builder.Property(x => x.Brand).HasColumnName("brand").HasMaxLength(100);
        builder.Property(x => x.Origin).HasColumnName("origin").HasMaxLength(100);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(x => x.TaxRate).HasColumnName("tax_rate").HasColumnType("numeric(8,4)");
        builder.Property(x => x.IsOnSale).HasColumnName("is_on_sale").HasDefaultValue(true);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_goods_code");
        builder.HasIndex(x => x.GoodsTypeId).HasDatabaseName("idx_goods_type_id");
        builder.HasIndex(x => x.DefaultSupplierId).HasDatabaseName("idx_goods_default_supplier_id");
        builder.HasIndex(x => x.DefaultWareId).HasDatabaseName("idx_goods_default_ware_id");

        builder.HasOne(x => x.GoodsType)
            .WithMany(x => x.Goods)
            .HasForeignKey(x => x.GoodsTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BaseUnit)
            .WithMany()
            .HasForeignKey(x => x.BaseUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DefaultSupplier)
            .WithMany()
            .HasForeignKey(x => x.DefaultSupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DefaultWare)
            .WithMany(x => x.DefaultGoods)
            .HasForeignKey(x => x.DefaultWareId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class GoodsUnitConfiguration : IEntityTypeConfiguration<GoodsUnit>
{
    public void Configure(EntityTypeBuilder<GoodsUnit> builder)
    {
        builder.ToTable("goods_unit");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
        builder.Property(x => x.ConversionRate)
            .HasColumnName("conversion_rate")
            .HasColumnType("numeric(18,6)")
            .HasDefaultValue(1m);
        builder.Property(x => x.IsBaseUnit).HasColumnName("is_base_unit");
        builder.Property(x => x.Sort).HasColumnName("sort");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_goods_unit_goods_id");
        builder.HasIndex(x => new { x.GoodsId, x.Name })
            .IsUnique()
            .HasDatabaseName("idx_goods_unit_goods_id_name");

        builder.HasOne(x => x.Goods)
            .WithMany(x => x.Units)
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GoodsImageConfiguration : IEntityTypeConfiguration<GoodsImage>
{
    public void Configure(EntityTypeBuilder<GoodsImage> builder)
    {
        builder.ToTable("goods_image");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.Url).HasColumnName("url").HasMaxLength(500).IsRequired();
        builder.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(200);
        builder.Property(x => x.Sort).HasColumnName("sort");
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary");

        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_goods_image_goods_id");

        builder.HasOne(x => x.Goods)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GoodsSupplierRelationConfiguration : IEntityTypeConfiguration<GoodsSupplierRelation>
{
    public void Configure(EntityTypeBuilder<GoodsSupplierRelation> builder)
    {
        builder.ToTable("goods_supplier_rel");

        builder.HasKey(x => new { x.GoodsId, x.SupplierId });

        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.IsDefault).HasColumnName("is_default");

        builder.HasIndex(x => x.SupplierId).HasDatabaseName("idx_goods_supplier_rel_supplier_id");

        builder.HasOne(x => x.Goods)
            .WithMany(x => x.SupplierRelations)
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.GoodsRelations)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

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

public class PurchaserConfiguration : IEntityTypeConfiguration<Purchaser>
{
    public void Configure(EntityTypeBuilder<Purchaser> builder)
    {
        builder.ToTable("purchaser");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.DepartmentId).HasColumnName("department_id");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_purchaser_code");
        builder.HasIndex(x => x.UserId).HasDatabaseName("idx_purchaser_user_id");
        builder.HasIndex(x => x.DepartmentId).HasDatabaseName("idx_purchaser_department_id");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class WareConfiguration : IEntityTypeConfiguration<Ware>
{
    public void Configure(EntityTypeBuilder<Ware> builder)
    {
        builder.ToTable("ware");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ContactName).HasColumnName("contact_name").HasMaxLength(50);
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(20);
        builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(300);
        builder.Property(x => x.Sort).HasColumnName("sort");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_ware_code");
        builder.HasIndex(x => x.Name).HasDatabaseName("idx_ware_name");
    }
}

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("company");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ContactName).HasColumnName("contact_name").HasMaxLength(50);
        builder.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(20);
        builder.Property(x => x.Address).HasColumnName("address").HasMaxLength(300);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_company_code");
        builder.HasIndex(x => x.Name).HasDatabaseName("idx_company_name");
    }
}

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

public class CustomerTagConfiguration : IEntityTypeConfiguration<CustomerTag>
{
    public void Configure(EntityTypeBuilder<CustomerTag> builder)
    {
        builder.ToTable("customer_tag");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ParentId).HasColumnName("parent_id");
        builder.Property(x => x.Sort).HasColumnName("sort");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_customer_tag_code");
        builder.HasIndex(x => x.ParentId).HasDatabaseName("idx_customer_tag_parent_id");

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

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

public class QuotationGoodsConfiguration : IEntityTypeConfiguration<QuotationGoods>
{
    public void Configure(EntityTypeBuilder<QuotationGoods> builder)
    {
        builder.ToTable("quotation_goods");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.QuotationId).HasColumnName("quotation_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.GoodsUnitId).HasColumnName("goods_unit_id");
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.MinOrderQuantity).HasColumnName("min_order_quantity").HasColumnType("numeric(18,4)");
        builder.Property(x => x.IsOnSale).HasColumnName("is_on_sale").HasDefaultValue(true);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.QuotationId).HasDatabaseName("idx_quotation_goods_quotation_id");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_quotation_goods_goods_id");
        builder.HasIndex(x => x.GoodsUnitId).HasDatabaseName("idx_quotation_goods_unit_id");
        builder.HasIndex(x => new { x.QuotationId, x.GoodsId, x.GoodsUnitId })
            .IsUnique()
            .HasDatabaseName("idx_quotation_goods_unique_goods_unit");

        builder.HasOne(x => x.Quotation)
            .WithMany(x => x.Goods)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Goods)
            .WithMany(x => x.QuotationGoods)
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GoodsUnit)
            .WithMany()
            .HasForeignKey(x => x.GoodsUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

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

public class CustomerProtocolGoodsConfiguration : IEntityTypeConfiguration<CustomerProtocolGoods>
{
    public void Configure(EntityTypeBuilder<CustomerProtocolGoods> builder)
    {
        builder.ToTable("customer_protocol_goods");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.CustomerProtocolId).HasColumnName("customer_protocol_id");
        builder.Property(x => x.GoodsId).HasColumnName("goods_id");
        builder.Property(x => x.GoodsUnitId).HasColumnName("goods_unit_id");
        builder.Property(x => x.ProtocolPrice).HasColumnName("protocol_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.MinOrderQuantity).HasColumnName("min_order_quantity").HasColumnType("numeric(18,4)");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.CustomerProtocolId).HasDatabaseName("idx_customer_protocol_goods_protocol_id");
        builder.HasIndex(x => x.GoodsId).HasDatabaseName("idx_customer_protocol_goods_goods_id");
        builder.HasIndex(x => x.GoodsUnitId).HasDatabaseName("idx_customer_protocol_goods_unit_id");
        builder.HasIndex(x => new { x.CustomerProtocolId, x.GoodsId, x.GoodsUnitId })
            .IsUnique()
            .HasDatabaseName("idx_customer_protocol_goods_unique_goods_unit");

        builder.HasOne(x => x.CustomerProtocol)
            .WithMany(x => x.Goods)
            .HasForeignKey(x => x.CustomerProtocolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Goods)
            .WithMany(x => x.CustomerProtocolGoods)
            .HasForeignKey(x => x.GoodsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GoodsUnit)
            .WithMany()
            .HasForeignKey(x => x.GoodsUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

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

public class PurchaseRuleConfiguration : IEntityTypeConfiguration<PurchaseRule>
{
    public void Configure(EntityTypeBuilder<PurchaseRule> builder)
    {
        builder.ToTable("purchase_rule");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.PurchaserId).HasColumnName("purchaser_id");
        builder.Property(x => x.WareId).HasColumnName("ware_id");
        builder.Property(x => x.GoodsTypeId).HasColumnName("goods_type_id");
        builder.Property(x => x.PurchasePattern).HasColumnName("purchase_pattern").HasDefaultValue(1);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("idx_purchase_rule_code");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("idx_purchase_rule_supplier_id");
        builder.HasIndex(x => x.PurchaserId).HasDatabaseName("idx_purchase_rule_purchaser_id");
        builder.HasIndex(x => x.WareId).HasDatabaseName("idx_purchase_rule_ware_id");
        builder.HasIndex(x => x.GoodsTypeId).HasDatabaseName("idx_purchase_rule_goods_type_id");

        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.PurchaseRules)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Purchaser)
            .WithMany(x => x.PurchaseRules)
            .HasForeignKey(x => x.PurchaserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Ware)
            .WithMany(x => x.PurchaseRules)
            .HasForeignKey(x => x.WareId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.GoodsType)
            .WithMany()
            .HasForeignKey(x => x.GoodsTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

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
