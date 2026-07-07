using Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Constants;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 供应商待结单据主表的数据库映射配置。
/// </summary>
public class SupplierBillConfiguration : IEntityTypeConfiguration<SupplierBill>
{
    /// <summary>
    /// 配置单据编号、供应商与出入库来源、金额精度、结款状态、唯一来源约束和业务外键。
    /// </summary>
    /// <param name="builder">供应商待结单据实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<SupplierBill> builder)
    {
        builder.ToTable("supplier_bill", table =>
        {
            table.HasCheckConstraint(
                "ck_supplier_bill_amounts",
                "document_amount >= 0 AND settled_amount >= 0 AND settled_amount <= document_amount AND ((source_type = 1 AND payable_amount >= 0) OR (source_type = 2 AND payable_amount <= 0))");
            table.HasCheckConstraint("ck_supplier_bill_status", "bill_status BETWEEN 1 AND 3");
            table.HasCheckConstraint(
                "ck_supplier_bill_source",
                "(source_type = 1 AND stock_in_order_id IS NOT NULL AND stock_out_order_id IS NULL) OR (source_type = 2 AND stock_out_order_id IS NOT NULL AND stock_in_order_id IS NULL)");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.BillNo).HasColumnName("bill_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
        builder.Property(x => x.SupplierNameSnapshot).HasColumnName("supplier_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.SourceType).HasColumnName("source_type").HasColumnType("integer").IsRequired();
        builder.Property(x => x.StockInOrderId).HasColumnName("stock_in_order_id");
        builder.Property(x => x.StockOutOrderId).HasColumnName("stock_out_order_id");
        builder.Property(x => x.SourceDocumentNoSnapshot).HasColumnName("source_document_no_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.BillDate).HasColumnName("bill_date").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.DocumentAmount).HasColumnName("document_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.PayableAmount).HasColumnName("payable_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.SettledAmount).HasColumnName("settled_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.BillStatus).HasColumnName("bill_status").HasColumnType("integer")
            .HasDefaultValue(SupplierBillStatus.Pending).IsRequired();
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.BillNo).IsUnique().HasDatabaseName("idx_supplier_bill_no");
        builder.HasIndex(x => x.StockInOrderId).IsUnique().HasDatabaseName("idx_supplier_bill_stock_in_order_id");
        builder.HasIndex(x => x.StockOutOrderId).IsUnique().HasDatabaseName("idx_supplier_bill_stock_out_order_id");
        builder.HasIndex(x => new { x.SupplierId, x.BillDate }).HasDatabaseName("idx_supplier_bill_supplier_date");
        builder.HasIndex(x => new { x.BillStatus, x.BillDate }).HasDatabaseName("idx_supplier_bill_status_date");

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockInOrder)
            .WithMany()
            .HasForeignKey(x => x.StockInOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockOutOrder)
            .WithMany()
            .HasForeignKey(x => x.StockOutOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
