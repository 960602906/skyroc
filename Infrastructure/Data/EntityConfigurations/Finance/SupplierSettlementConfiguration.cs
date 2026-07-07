using Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Constants;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 供应商结算单主表的数据库映射配置。
/// </summary>
public class SupplierSettlementConfiguration : IEntityTypeConfiguration<SupplierSettlement>
{
    /// <summary>
    /// 配置结算单编号、供应商快照、金额精度、状态约束、作废审计和供应商外键。
    /// </summary>
    /// <param name="builder">供应商结算单实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<SupplierSettlement> builder)
    {
        builder.ToTable("supplier_settlement", table =>
        {
            table.HasCheckConstraint(
                "ck_supplier_settlement_amounts",
                "should_amount >= 0 AND payment_amount >= 0 AND discount_amount >= 0 AND applied_amount = payment_amount + discount_amount AND remaining_amount >= 0");
            table.HasCheckConstraint("ck_supplier_settlement_status", "settlement_status IN (-1, 1, 2, 3)");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.SettlementNo).HasColumnName("settlement_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
        builder.Property(x => x.SupplierNameSnapshot).HasColumnName("supplier_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.SettlementDate).HasColumnName("settlement_date").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.SerialNo).HasColumnName("serial_no").HasMaxLength(100);
        builder.Property(x => x.ShouldAmount).HasColumnName("should_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.PaymentAmount).HasColumnName("payment_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.DiscountAmount).HasColumnName("discount_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.AppliedAmount).HasColumnName("applied_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.RemainingAmount).HasColumnName("remaining_amount")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.SettlementStatus).HasColumnName("settlement_status").HasColumnType("integer")
            .HasDefaultValue(SupplierSettlementStatus.Pending).IsRequired();
        builder.Property(x => x.VoidedTime).HasColumnName("voided_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.VoidedBy).HasColumnName("voided_by");
        builder.Property(x => x.VoidedByNameSnapshot).HasColumnName("voided_by_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.SettlementNo).IsUnique().HasDatabaseName("idx_supplier_settlement_no");
        builder.HasIndex(x => new { x.SupplierId, x.SettlementDate }).HasDatabaseName("idx_supplier_settlement_supplier_date");
        builder.HasIndex(x => new { x.SettlementStatus, x.SettlementDate }).HasDatabaseName("idx_supplier_settlement_status_date");
        builder.HasIndex(x => x.SerialNo).HasDatabaseName("idx_supplier_settlement_serial_no");

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
