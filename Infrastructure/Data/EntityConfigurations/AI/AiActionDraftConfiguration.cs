using Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.EntityConfigurations;

/// <summary>
/// AI 通用操作草稿的 PostgreSQL 映射配置。
/// </summary>
public class AiActionDraftConfiguration : IEntityTypeConfiguration<AiActionDraft>
{
    /// <summary>
    /// 配置用户隔离、不可变参数哈希、确认状态、幂等和并发约束。
    /// </summary>
    public void Configure(EntityTypeBuilder<AiActionDraft> builder)
    {
        builder.ToTable("ai_action_draft", table =>
        {
            table.HasCheckConstraint("ck_ai_action_draft_risk", "risk_level BETWEEN 1 AND 2");
            table.HasCheckConstraint("ck_ai_action_draft_status", "draft_status BETWEEN 1 AND 7");
            table.HasCheckConstraint("ck_ai_action_draft_hash", "char_length(arguments_hash) = 64");
            table.HasCheckConstraint("ck_ai_action_draft_operation", "position('.' in operation_id) > 1");
            table.HasCheckConstraint("ck_ai_action_draft_concurrency_version", "concurrency_version > 0");
            table.HasCheckConstraint(
                "ck_ai_action_draft_confirmation",
                "draft_status IN (1, 6, 7) OR (confirmed_by_user_id IS NOT NULL AND confirmed_time IS NOT NULL)");
            table.HasCheckConstraint(
                "ck_ai_action_draft_confirmed_owner",
                "confirmed_by_user_id IS NULL OR confirmed_by_user_id = user_id");
        });
        builder.ConfigureBaseEntity();

        builder.Property(x => x.ConversationId).HasColumnName("conversation_id");
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.OperationId).HasColumnName("operation_id").HasMaxLength(200).IsRequired();
        builder.Property(x => x.CanonicalArgumentsJson).HasColumnName("canonical_arguments_json")
            .HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ArgumentsHash).HasColumnName("arguments_hash")
            .HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(x => x.RiskLevel).HasColumnName("risk_level").HasColumnType("integer")
            .HasDefaultValue(AiActionDraftRiskLevel.Write)
            .HasSentinel((AiActionDraftRiskLevel)0)
            .IsRequired();
        builder.Property(x => x.ConfirmationSummary).HasColumnName("confirmation_summary")
            .HasMaxLength(1000).IsRequired();
        builder.Property(x => x.DraftStatus).HasColumnName("draft_status").HasColumnType("integer")
            .HasDefaultValue(AiActionDraftStatus.PendingConfirmation)
            .HasSentinel((AiActionDraftStatus)0)
            .IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP + INTERVAL '30 minutes'")
            .IsRequired();
        builder.Property(x => x.ConfirmedByUserId).HasColumnName("confirmed_by_user_id");
        builder.Property(x => x.ConfirmedTime).HasColumnName("confirmed_time")
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.ExecutedTime).HasColumnName("executed_time")
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.ExecutionResultReference).HasColumnName("execution_result_reference")
            .HasMaxLength(200);
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.ConcurrencyVersion).HasColumnName("concurrency_version")
            .HasDefaultValue(1L)
            .HasSentinel(0L)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("idx_ai_action_draft_user_idempotency");
        builder.HasIndex(x => new { x.UserId, x.DraftStatus, x.ExpiresAt })
            .HasDatabaseName("idx_ai_action_draft_user_status_expires_at");
        builder.HasIndex(x => new { x.DraftStatus, x.ExpiresAt })
            .HasDatabaseName("idx_ai_action_draft_status_expires_at");
        builder.HasIndex(x => new { x.OperationId, x.CreateTime, x.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("idx_ai_action_draft_operation_create_time");

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.ActionDrafts)
            .HasForeignKey(x => new { x.ConversationId, x.UserId })
            .HasPrincipalKey(x => new { x.Id, x.UserId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ConfirmedByUser)
            .WithMany()
            .HasForeignKey(x => x.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
