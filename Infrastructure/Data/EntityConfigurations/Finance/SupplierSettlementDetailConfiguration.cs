using Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Constants;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 供应商结算单明细的数据库映射配置。
/// </summary>
public class SupplierSettlementDetailConfiguration : IEntityTypeConfiguration<SupplierSettlementDetail>
{
    /// <summary>
    /// 配置待结单据来源快照、金额精度、单结算单单据唯一约束和历史外键保护。
    /// </summary>
    /// <param name="builder">供应商结算单明细实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<SupplierSettlementDetail> builder)
    {
        builder.ToTable("supplier_settlement_detail", table =>
        {
            table.HasCheckConstraint(
                "ck_supplier_settlement_detail_amounts",
                "payable_amount_snapshot <> 0 AND previous_settled_amount >= 0 AND payment_amount >= 0 AND discount_amount >= 0 AND applied_amount = payment_amount + discount_amount AND current_settled_amount >= previous_settled_amount AND remaining_amount >= 0");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.SupplierSettlementId).HasColumnName("supplier_settlement_id").IsRequired();
        builder.Property(x => x.SupplierBillId).HasColumnName("supplier_bill_id").IsRequired();
        builder.Property(x => x.SupplierBillNoSnapshot).HasColumnName("supplier_bill_no_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SourceType).HasColumnName("source_type").HasColumnType("integer").IsRequired();
        builder.Property(x => x.SourceDocumentNoSnapshot).HasColumnName("source_document_no_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.StockInOrderId).HasColumnName("stock_in_order_id");
        builder.Property(x => x.StockOutOrderId).HasColumnName("stock_out_order_id");
        builder.Property(x => x.PayableAmountSnapshot).HasColumnName("payable_amount_snapshot")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.PreviousSettledAmount).HasColumnName("previous_settled_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.PaymentAmount).HasColumnName("payment_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.AppliedAmount).HasColumnName("applied_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.CurrentSettledAmount).HasColumnName("current_settled_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.RemainingAmount).HasColumnName("remaining_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => new { x.SupplierSettlementId, x.SupplierBillId })
            .IsUnique()
            .HasDatabaseName("idx_supplier_settlement_detail_settlement_bill");
        builder.HasIndex(x => x.SupplierBillId).HasDatabaseName("idx_supplier_settlement_detail_bill_id");
        builder.HasIndex(x => x.StockInOrderId).HasDatabaseName("idx_supplier_settlement_detail_stock_in_order_id");
        builder.HasIndex(x => x.StockOutOrderId).HasDatabaseName("idx_supplier_settlement_detail_stock_out_order_id");

        builder.HasOne(x => x.SupplierSettlement)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.SupplierSettlementId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SupplierBill)
            .WithMany()
            .HasForeignKey(x => x.SupplierBillId)
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
