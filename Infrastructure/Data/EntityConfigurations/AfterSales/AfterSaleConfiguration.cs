using Domain.Entities.AfterSales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Constants;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 售后单主表的数据库映射配置。
/// </summary>
public class AfterSaleConfiguration : IEntityTypeConfiguration<AfterSale>
{
    /// <summary>
    /// 配置售后编号、来源快照、状态、金额约束、查询索引和业务外键。
    /// </summary>
    /// <param name="builder">售后单实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<AfterSale> builder)
    {
        builder.ToTable("after_sale", table =>
        {
            table.HasCheckConstraint(
                "ck_after_sale_amounts",
                "order_price >= 0 AND settlement_price >= 0");
            table.HasCheckConstraint("ck_after_sale_status", "after_status BETWEEN 1 AND 5");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.AfterSaleNo).HasColumnName("after_sale_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SaleOrderId).HasColumnName("sale_order_id");
        builder.Property(x => x.SaleOrderNoSnapshot).HasColumnName("sale_order_no_snapshot").HasMaxLength(50);
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.CustomerNameSnapshot).HasColumnName("customer_name_snapshot").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
        builder.Property(x => x.AfterStatus).HasColumnName("after_status").HasColumnType("integer")
            .HasDefaultValue(AfterSaleStatus.Draft)
            .HasSentinel((AfterSaleStatus)0)
            .IsRequired();
        builder.Property(x => x.OrderPrice).HasColumnName("order_price")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.SettlementPrice).HasColumnName("settlement_price")
            .HasPrecision(18, NumericPrecision.MoneyScale).IsRequired();
        builder.Property(x => x.ContactNameSnapshot).HasColumnName("contact_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.ContactPhoneSnapshot).HasColumnName("contact_phone_snapshot").HasMaxLength(30);
        builder.Property(x => x.PickupAddressSnapshot).HasColumnName("pickup_address_snapshot").HasMaxLength(500);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);

        builder.HasIndex(x => x.AfterSaleNo).IsUnique().HasDatabaseName("idx_after_sale_no");
        builder.HasIndex(x => x.SaleOrderId).HasDatabaseName("idx_after_sale_order_id");
        builder.HasIndex(x => x.CustomerId).HasDatabaseName("idx_after_sale_customer_id");
        builder.HasIndex(x => new { x.AfterStatus, x.CreateTime }).HasDatabaseName("idx_after_sale_status_create_time");
        builder.HasIndex(x => new { x.CreateTime, x.Id })
            .IsDescending(true, true)
            .HasDatabaseName("idx_after_sale_create_time_id");

        builder.HasOne(x => x.SaleOrder)
            .WithMany()
            .HasForeignKey(x => x.SaleOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
