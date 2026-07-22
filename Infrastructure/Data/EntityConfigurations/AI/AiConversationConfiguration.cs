using Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// AI 会话的 PostgreSQL 映射配置。
/// </summary>
public class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    /// <summary>
    /// 配置用户隔离键、保留期限、会话列表索引和生命周期约束。
    /// </summary>
    /// <param name="builder">AI 会话实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        builder.ToTable("ai_conversation", table =>
        {
            table.HasCheckConstraint("ck_ai_conversation_status", "conversation_status IN (1, 2)");
            table.HasCheckConstraint(
                "ck_ai_conversation_deleted_time",
                "conversation_status = 1 OR deleted_time IS NOT NULL");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ConversationStatus).HasColumnName("conversation_status").HasColumnType("integer")
            .HasDefaultValue(AiConversationStatus.Active)
            .HasSentinel((AiConversationStatus)0)
            .IsRequired();
        builder.Property(x => x.LastMessageTime).HasColumnName("last_message_time")
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.RetainUntil).HasColumnName("retain_until")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP + INTERVAL '30 days'")
            .IsRequired();
        builder.Property(x => x.DeletedTime).HasColumnName("deleted_time")
            .HasColumnType("timestamp with time zone");

        builder.HasAlternateKey(x => new { x.Id, x.UserId })
            .HasName("ak_ai_conversation_id_user_id");
        builder.HasIndex(x => new { x.UserId, x.LastMessageTime, x.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("idx_ai_conversation_user_last_message");
        builder.HasIndex(x => new { x.ConversationStatus, x.RetainUntil })
            .HasDatabaseName("idx_ai_conversation_status_retain_until");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
