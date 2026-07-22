using Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// AI 订单草稿主表的 PostgreSQL 映射配置。
/// </summary>
public class AiOrderDraftConfiguration : IEntityTypeConfiguration<AiOrderDraft>
{
    /// <summary>
    /// 配置草稿归属、确认状态、幂等键、并发版本和业务外键。
    /// </summary>
    /// <param name="builder">AI 订单草稿实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<AiOrderDraft> builder)
    {
        builder.ToTable("ai_order_draft", table =>
        {
            table.HasCheckConstraint("ck_ai_order_draft_status", "draft_status BETWEEN 1 AND 4");
            table.HasCheckConstraint("ck_ai_order_draft_concurrency_version", "concurrency_version > 0");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.ConversationId).HasColumnName("conversation_id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(x => x.CustomerNameSnapshot).HasColumnName("customer_name_snapshot")
            .HasMaxLength(150).IsRequired();
        builder.Property(x => x.CustomerCodeSnapshot).HasColumnName("customer_code_snapshot")
            .HasMaxLength(50).IsRequired();
        builder.Property(x => x.QuotationId).HasColumnName("quotation_id");
        builder.Property(x => x.WareId).HasColumnName("ware_id");
        builder.Property(x => x.OrderDate).HasColumnName("order_date")
            .HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ReceiveDate).HasColumnName("receive_date")
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.ContactNameSnapshot).HasColumnName("contact_name_snapshot").HasMaxLength(100);
        builder.Property(x => x.ContactPhoneSnapshot).HasColumnName("contact_phone_snapshot").HasMaxLength(30);
        builder.Property(x => x.DeliveryAddressSnapshot).HasColumnName("delivery_address_snapshot").HasMaxLength(500);
        builder.Property(x => x.Remark).HasColumnName("remark").HasMaxLength(500);
        builder.Property(x => x.DraftStatus).HasColumnName("draft_status").HasColumnType("integer")
            .HasDefaultValue(AiOrderDraftStatus.PendingConfirmation)
            .HasSentinel((AiOrderDraftStatus)0)
            .IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP + INTERVAL '30 minutes'")
            .IsRequired();
        builder.Property(x => x.ConfirmedTime).HasColumnName("confirmed_time")
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.SaleOrderId).HasColumnName("sale_order_id");
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.ConcurrencyVersion).HasColumnName("concurrency_version")
            .HasDefaultValue(1L)
            .HasSentinel(0L)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("idx_ai_order_draft_user_idempotency");
        builder.HasIndex(x => x.SaleOrderId)
            .IsUnique()
            .HasDatabaseName("idx_ai_order_draft_sale_order_id");
        builder.HasIndex(x => new { x.UserId, x.DraftStatus, x.ExpiresAt })
            .HasDatabaseName("idx_ai_order_draft_user_status_expires_at");
        builder.HasIndex(x => new { x.DraftStatus, x.ExpiresAt })
            .HasDatabaseName("idx_ai_order_draft_status_expires_at");

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.OrderDrafts)
            .HasForeignKey(x => new { x.ConversationId, x.UserId })
            .HasPrincipalKey(x => new { x.Id, x.UserId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
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
        builder.HasOne(x => x.SaleOrder)
            .WithMany()
            .HasForeignKey(x => x.SaleOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
