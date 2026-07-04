using Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// 订单签收回单的数据库映射配置。
/// </summary>
public class OrderReceiptConfiguration : IEntityTypeConfiguration<OrderReceipt>
{
    /// <summary>
    /// 配置回单编号、签收与归档字段、唯一任务约束及业务外键。
    /// </summary>
    /// <param name="builder">签收回单实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<OrderReceipt> builder)
    {
        builder.ToTable("order_receipt");
        builder.ConfigureBaseEntity();

        builder.Property(x => x.ReceiptNo).HasColumnName("receipt_no").HasMaxLength(50).IsRequired();
        builder.Property(x => x.DeliveryTaskId).HasColumnName("delivery_task_id").IsRequired();
        builder.Property(x => x.SaleOrderId).HasColumnName("sale_order_id").IsRequired();
        builder.Property(x => x.StockOutOrderId).HasColumnName("stock_out_order_id").IsRequired();
        builder.Property(x => x.SignerName).HasColumnName("signer_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.SignedTime).HasColumnName("signed_time").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.SignRemark).HasColumnName("sign_remark").HasMaxLength(500);
        builder.Property(x => x.ReceiptImageUrl).HasColumnName("receipt_image_url").HasMaxLength(1000);
        builder.Property(x => x.ReturnedTime).HasColumnName("returned_time").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReturnRemark).HasColumnName("return_remark").HasMaxLength(500);

        builder.HasIndex(x => x.ReceiptNo).IsUnique().HasDatabaseName("idx_order_receipt_no");
        builder.HasIndex(x => x.DeliveryTaskId).IsUnique().HasDatabaseName("idx_order_receipt_delivery_task_id");
        builder.HasIndex(x => new { x.SaleOrderId, x.ReturnedTime }).HasDatabaseName("idx_order_receipt_order_returned_time");

        builder.HasOne(x => x.DeliveryTask)
            .WithOne(x => x.Receipt)
            .HasForeignKey<OrderReceipt>(x => x.DeliveryTaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SaleOrder)
            .WithMany(x => x.Receipts)
            .HasForeignKey(x => x.SaleOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockOutOrder)
            .WithMany()
            .HasForeignKey(x => x.StockOutOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
