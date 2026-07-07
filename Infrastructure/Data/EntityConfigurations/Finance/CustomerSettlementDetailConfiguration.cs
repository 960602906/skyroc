using Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Constants;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 客户结款凭证明细的数据库映射配置。
/// </summary>
public class CustomerSettlementDetailConfiguration : IEntityTypeConfiguration<CustomerSettlementDetail>
{
    /// <summary>
    /// 配置账单来源快照、金额精度、单凭证账单唯一约束和历史外键保护。
    /// </summary>
    /// <param name="builder">客户结款凭证明细实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<CustomerSettlementDetail> builder)
    {
        builder.ToTable("customer_settlement_detail", table =>
        {
            table.HasCheckConstraint(
                "ck_customer_settlement_detail_amounts",
                "receivable_amount_snapshot >= 0 AND previous_settled_amount >= 0 AND payment_amount >= 0 AND discount_amount >= 0 AND applied_amount = payment_amount + discount_amount AND current_settled_amount >= previous_settled_amount AND remaining_amount >= 0");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.CustomerSettlementId).HasColumnName("customer_settlement_id").IsRequired();
        builder.Property(x => x.CustomerBillId).HasColumnName("customer_bill_id").IsRequired();
        builder.Property(x => x.CustomerBillNoSnapshot).HasColumnName("customer_bill_no_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SaleOrderId).HasColumnName("sale_order_id").IsRequired();
        builder.Property(x => x.SaleOrderNoSnapshot).HasColumnName("sale_order_no_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReceivableAmountSnapshot).HasColumnName("receivable_amount_snapshot")
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

        builder.HasIndex(x => new { x.CustomerSettlementId, x.CustomerBillId })
            .IsUnique()
            .HasDatabaseName("idx_customer_settlement_detail_settlement_bill");
        builder.HasIndex(x => x.CustomerBillId).HasDatabaseName("idx_customer_settlement_detail_bill_id");
        builder.HasIndex(x => x.SaleOrderId).HasDatabaseName("idx_customer_settlement_detail_sale_order_id");

        builder.HasOne(x => x.CustomerSettlement)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.CustomerSettlementId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.CustomerBill)
            .WithMany()
            .HasForeignKey(x => x.CustomerBillId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SaleOrder)
            .WithMany()
            .HasForeignKey(x => x.SaleOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
