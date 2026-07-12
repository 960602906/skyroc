using Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Constants;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 客户账单主表的数据库映射配置。
/// </summary>
public class CustomerBillConfiguration : IEntityTypeConfiguration<CustomerBill>
{
    /// <summary>
    /// 配置账单编号、客户与订单来源、金额精度、结款状态、唯一来源约束和业务外键。
    /// </summary>
    /// <param name="builder">客户账单实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<CustomerBill> builder)
    {
        builder.ToTable("customer_bill", table =>
        {
            table.HasCheckConstraint(
                "ck_customer_bill_amounts",
                "order_amount >= 0 AND after_sale_adjustment_amount <= 0 AND receivable_amount >= 0 AND settled_amount >= 0 AND settled_amount <= receivable_amount");
            table.HasCheckConstraint("ck_customer_bill_status", "bill_status BETWEEN 1 AND 3");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.BillNo).HasColumnName("bill_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.CustomerNameSnapshot).HasColumnName("customer_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.SaleOrderId).HasColumnName("sale_order_id").IsRequired();
        builder.Property(x => x.SaleOrderNoSnapshot).HasColumnName("sale_order_no_snapshot").HasMaxLength(50).IsRequired();
        builder.Property(x => x.BillDate).HasColumnName("bill_date").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.OrderAmount).HasColumnName("order_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.AfterSaleAdjustmentAmount).HasColumnName("after_sale_adjustment_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.ReceivableAmount).HasColumnName("receivable_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.SettledAmount).HasColumnName("settled_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.BillStatus).HasColumnName("bill_status").HasColumnType("integer")
            .HasDefaultValue(CustomerBillStatus.Pending)
            .HasSentinel((CustomerBillStatus)0)
            .IsRequired();
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.BillNo).IsUnique().HasDatabaseName("idx_customer_bill_no");
        builder.HasIndex(x => x.SaleOrderId).IsUnique().HasDatabaseName("idx_customer_bill_sale_order_id");
        builder.HasIndex(x => new { x.CustomerId, x.BillDate }).HasDatabaseName("idx_customer_bill_customer_date");
        builder.HasIndex(x => new { x.BillStatus, x.BillDate }).HasDatabaseName("idx_customer_bill_status_date");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SaleOrder)
            .WithMany()
            .HasForeignKey(x => x.SaleOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
