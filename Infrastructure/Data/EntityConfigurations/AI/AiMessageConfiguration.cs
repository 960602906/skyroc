using Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// AI 会话消息的 PostgreSQL 映射配置。
/// </summary>
public class AiMessageConfiguration : IEntityTypeConfiguration<AiMessage>
{
    /// <summary>
    /// 配置会话内游标、消息终态和脱敏工具摘要字段。
    /// </summary>
    /// <param name="builder">AI 消息实体类型构建器。</param>
    public void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        builder.ToTable("ai_message", table =>
        {
            table.HasCheckConstraint("ck_ai_message_role", "role BETWEEN 1 AND 4");
            table.HasCheckConstraint("ck_ai_message_status", "message_status BETWEEN 1 AND 4");
            table.HasCheckConstraint("ck_ai_message_sequence", "sequence > 0");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.ConversationId).HasColumnName("conversation_id").IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").HasColumnType("integer").IsRequired();
        builder.Property(x => x.Content).HasColumnName("content").HasColumnType("text").IsRequired();
        builder.Property(x => x.Sequence).HasColumnName("sequence").IsRequired();
        builder.Property(x => x.MessageStatus).HasColumnName("message_status").HasColumnType("integer")
            .HasDefaultValue(AiMessageStatus.Pending)
            .HasSentinel((AiMessageStatus)0)
            .IsRequired();
        builder.Property(x => x.ProviderName).HasColumnName("provider_name").HasMaxLength(64);
        builder.Property(x => x.ModelName).HasColumnName("model_name").HasMaxLength(128);
        builder.Property(x => x.ToolCallId).HasColumnName("tool_call_id").HasMaxLength(100);
        builder.Property(x => x.ToolName).HasColumnName("tool_name").HasMaxLength(100);
        builder.Property(x => x.ToolStatus).HasColumnName("tool_status").HasMaxLength(32);
        builder.Property(x => x.ToolSummary).HasColumnName("tool_summary").HasMaxLength(500);
        builder.Property(x => x.SourceReferences).HasColumnName("source_references").HasColumnType("jsonb");
        builder.Property(x => x.CompletedTime).HasColumnName("completed_time")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.ConversationId, x.Sequence })
            .IsUnique()
            .HasDatabaseName("idx_ai_message_conversation_sequence");

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
