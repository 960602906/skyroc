using Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

public class SaleOrderConfiguration : IEntityTypeConfiguration<SaleOrder>
{
    public void Configure(EntityTypeBuilder<SaleOrder> builder)
    {
        builder.ToTable("sale_order");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.OrderNo).HasColumnName("order_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.CustomerNameSnapshot).HasColumnName("customer_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.CustomerCodeSnapshot).HasColumnName("customer_code_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.QuotationId).HasColumnName("quotation_id");
        builder.Property(x => x.WareId).HasColumnName("ware_id");
        builder.Property(x => x.OrderDate).HasColumnName("order_date").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReceiveDate).HasColumnName("receive_date").HasColumnType("timestamp with time zone");
        builder.Property(x => x.OutDate).HasColumnName("out_date").HasColumnType("timestamp with time zone");
        builder.Property(x => x.OrderStatus).HasColumnName("order_status").HasColumnType("integer").HasDefaultValue(SaleOrderStatus.PendingAudit);
        builder.Property(x => x.ReturnStatus).HasColumnName("return_status").HasColumnType("integer").HasDefaultValue(OrderReturnStatus.NotReturned);
        builder.Property(x => x.PrintStatus).HasColumnName("print_status").HasColumnType("integer").HasDefaultValue(OrderPrintStatus.NotPrinted);
        builder.Property(x => x.OutStorageStatus).HasColumnName("out_storage_status").HasColumnType("integer").HasDefaultValue(OrderOutStorageStatus.NotGenerated);
        builder.Property(x => x.OrderPrice).HasColumnName("order_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.SettlementPrice).HasColumnName("settlement_price").HasColumnType("numeric(18,4)");
        builder.Property(x => x.HasOutSale).HasColumnName("has_out_sale");
        builder.Property(x => x.UpdateStatus).HasColumnName("update_status");
        builder.Property(x => x.HasPurchasePlan).HasColumnName("has_purchase_plan");
        builder.Property(x => x.ContactNameSnapshot).HasColumnName("contact_name_snapshot").HasMaxLength(50);
        builder.Property(x => x.ContactPhoneSnapshot).HasColumnName("contact_phone_snapshot").HasMaxLength(20);
        builder.Property(x => x.DeliveryAddressSnapshot).HasColumnName("delivery_address_snapshot").HasMaxLength(300);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);
        builder.Property(x => x.InnerRemark).HasColumnName("inner_remark").HasMaxLength(500);

        builder.HasIndex(x => x.OrderNo).IsUnique().HasDatabaseName("idx_sale_order_order_no");
        builder.HasIndex(x => new { x.OrderDate, x.OrderStatus }).HasDatabaseName("idx_sale_order_date_status");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("idx_sale_order_customer_id");
        builder.HasIndex(x => x.QuotationId).HasDatabaseName("idx_sale_order_quotation_id");
        builder.HasIndex(x => x.WareId).HasDatabaseName("idx_sale_order_ware_id");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Quotation)
            .WithMany()
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Ware)
            .WithMany()
            .HasForeignKey(x => x.WareId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
