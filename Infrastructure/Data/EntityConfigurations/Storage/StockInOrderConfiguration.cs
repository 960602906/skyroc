using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations.Storage;

public class StockInOrderConfiguration : IEntityTypeConfiguration<StockInOrder>
{
    public void Configure(EntityTypeBuilder<StockInOrder> builder)
    {
        builder.ToTable(
            "stock_in_order",
            table =>
            {
                table.HasCheckConstraint("ck_stock_in_order_total_quantity", "total_base_quantity >= 0");
                table.HasCheckConstraint("ck_stock_in_order_total_amount", "total_amount >= 0");
            });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.InNo).HasColumnName("in_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.OrderType).HasColumnName("order_type").HasColumnType("integer");
        // Draft=1，CLR 默认 0 不是合法状态；显式 sentinel 避免 EF 20601，并在值为 0 时使用库默认 Draft
        builder.Property(x => x.BusinessStatus)
            .HasColumnName("business_status")
            .HasColumnType("integer")
            .HasDefaultValue(StockDocumentStatus.Draft)
            .HasSentinel((StockDocumentStatus)0);
        builder.Property(x => x.WareId).HasColumnName("ware_id");
        builder.Property(x => x.WareNameSnapshot).HasColumnName("ware_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.PurchaseOrderId).HasColumnName("purchase_order_id");
        builder.Property(x => x.AfterSaleId).HasColumnName("after_sale_id");
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id");
        builder.Property(x => x.SupplierNameSnapshot).HasColumnName("supplier_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.CustomerNameSnapshot).HasColumnName("customer_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.DepartmentId).HasColumnName("department_id");
        builder.Property(x => x.DepartmentNameSnapshot).HasColumnName("department_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.PurchaserId).HasColumnName("purchaser_id");
        builder.Property(x => x.PurchaserNameSnapshot).HasColumnName("purchaser_name_snapshot").HasMaxLength(150);
        builder.Property(x => x.PurchasePattern).HasColumnName("purchase_pattern").HasColumnType("integer");
        builder.Property(x => x.InTime).HasColumnName("in_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ExpectedArrivalTime).HasColumnName("expected_arrival_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.TotalBaseQuantity).HasColumnName("total_base_quantity").HasColumnType("numeric(18,6)");
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("numeric(18,4)");
        builder.Property(x => x.PrintStatus).HasColumnName("print_status").HasColumnType("integer").HasDefaultValue(StockPrintStatus.NotPrinted);
        builder.Property(x => x.AuditUserId).HasColumnName("audit_user_id");
        builder.Property(x => x.AuditUserNameSnapshot).HasColumnName("audit_user_name_snapshot").HasMaxLength(50);
        builder.Property(x => x.AuditTime).HasColumnName("audit_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReverseUserId).HasColumnName("reverse_user_id");
        builder.Property(x => x.ReverseUserNameSnapshot).HasColumnName("reverse_user_name_snapshot").HasMaxLength(50);
        builder.Property(x => x.ReverseTime).HasColumnName("reverse_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.InNo).IsUnique().HasDatabaseName("idx_stock_in_order_in_no");
        builder.HasIndex(x => new { x.WareId, x.BusinessStatus, x.InTime }).HasDatabaseName("idx_stock_in_order_ware_status_time");
        builder.HasIndex(x => x.PurchaseOrderId).HasDatabaseName("idx_stock_in_order_purchase_order_id");
        builder.HasIndex(x => x.AfterSaleId).HasDatabaseName("idx_stock_in_order_after_sale_id");

        builder.HasOne(x => x.Ware).WithMany().HasForeignKey(x => x.WareId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AfterSale).WithMany().HasForeignKey(x => x.AfterSaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Purchaser).WithMany().HasForeignKey(x => x.PurchaserId).OnDelete(DeleteBehavior.SetNull);
    }
}
