using Domain.Entities;
using Domain.Entities.AI;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Pricing;
using Domain.Entities.Storage;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shared.Constants;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.AI;

/// <summary>
/// 校验 AI 会话、消息、订单草稿和 MCP Token 的 EF Core 持久化契约。
/// </summary>
public class AiPersistenceModelTests
{
    private readonly IModel model;

    /// <summary>
    /// 构建设计期 PostgreSQL 模型用于结构断言，不建立真实数据库连接。
    /// </summary>
    public AiPersistenceModelTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=skyroc_ai_model_tests;Username=test;Password=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        model = context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void AiPersistenceEntities_MapToExpectedTables()
    {
        Assert.Equal("ai_conversation", GetEntityType<AiConversation>().GetTableName());
        Assert.Equal("ai_message", GetEntityType<AiMessage>().GetTableName());
        Assert.Equal("ai_action_draft", GetEntityType<AiActionDraft>().GetTableName());
        Assert.Equal("ai_order_draft", GetEntityType<AiOrderDraft>().GetTableName());
        Assert.Equal("ai_order_draft_detail", GetEntityType<AiOrderDraftDetail>().GetTableName());
        Assert.Equal("mcp_access_token", GetEntityType<McpAccessToken>().GetTableName());
    }

    [Fact]
    public void Conversation_ConfiguresThirtyDayRetentionUserIsolationAndListIndexes()
    {
        var entityType = GetEntityType<AiConversation>();

        Assert.Equal(
            AiConversationStatus.Active,
            entityType.FindProperty(nameof(AiConversation.ConversationStatus))!.GetDefaultValue());
        Assert.Equal(
            "CURRENT_TIMESTAMP + INTERVAL '30 days'",
            entityType.FindProperty(nameof(AiConversation.RetainUntil))!.GetDefaultValueSql());
        Assert.Contains(
            entityType.GetKeys(),
            key => key.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(AiConversation.Id), nameof(AiConversation.UserId)]));

        var listIndex = entityType.GetIndexes().Single(
            index => index.GetDatabaseName() == "idx_ai_conversation_user_last_message");
        Assert.Equal(
            [nameof(AiConversation.UserId), nameof(AiConversation.LastMessageTime), nameof(AiConversation.Id)],
            listIndex.Properties.Select(property => property.Name));
        Assert.Equal([false, true, true], listIndex.IsDescending);

        AssertForeignKey<AiConversation, User>([nameof(AiConversation.UserId)], DeleteBehavior.Restrict);
    }

    [Fact]
    public void Message_ConfiguresUniqueCursorAndStoresOnlySafeSummaries()
    {
        var entityType = GetEntityType<AiMessage>();
        var cursorIndex = entityType.GetIndexes().Single(
            index => index.GetDatabaseName() == "idx_ai_message_conversation_sequence");

        Assert.True(cursorIndex.IsUnique);
        Assert.Equal(
            [nameof(AiMessage.ConversationId), nameof(AiMessage.Sequence)],
            cursorIndex.Properties.Select(property => property.Name));
        Assert.Equal("text", entityType.FindProperty(nameof(AiMessage.Content))!.GetColumnType());
        Assert.Equal("jsonb", entityType.FindProperty(nameof(AiMessage.SourceReferences))!.GetColumnType());
        Assert.Equal(
            AiMessageStatus.Pending,
            entityType.FindProperty(nameof(AiMessage.MessageStatus))!.GetDefaultValue());
        AssertForeignKey<AiMessage, AiConversation>([nameof(AiMessage.ConversationId)], DeleteBehavior.Cascade);

        var forbiddenPropertyNames = new[]
        {
            "ReasoningContent",
            "ThinkingContent",
            "RawResponse",
            "AuthorizationHeader",
            "ToolRawResult"
        };
        Assert.DoesNotContain(
            typeof(AiMessage).GetProperties(),
            property => forbiddenPropertyNames.Contains(property.Name, StringComparer.Ordinal));
    }

    [Fact]
    public void ActionDraft_ConfiguresImmutableHashUserIsolationAndIdempotency()
    {
        var entityType = GetEntityType<AiActionDraft>();

        Assert.Equal(
            AiActionDraftStatus.PendingConfirmation,
            entityType.FindProperty(nameof(AiActionDraft.DraftStatus))!.GetDefaultValue());
        Assert.Equal(
            "CURRENT_TIMESTAMP + INTERVAL '30 minutes'",
            entityType.FindProperty(nameof(AiActionDraft.ExpiresAt))!.GetDefaultValueSql());
        Assert.Equal("jsonb", entityType.FindProperty(nameof(AiActionDraft.CanonicalArgumentsJson))!.GetColumnType());
        Assert.Equal(64, entityType.FindProperty(nameof(AiActionDraft.ArgumentsHash))!.GetMaxLength());
        Assert.True(entityType.FindProperty(nameof(AiActionDraft.ConcurrencyVersion))!.IsConcurrencyToken);

        var idempotencyIndex = entityType.GetIndexes().Single(
            index => index.GetDatabaseName() == "idx_ai_action_draft_user_idempotency");
        Assert.True(idempotencyIndex.IsUnique);
        Assert.Equal(
            [nameof(AiActionDraft.UserId), nameof(AiActionDraft.IdempotencyKey)],
            idempotencyIndex.Properties.Select(property => property.Name));

        var conversationForeignKey = entityType.GetForeignKeys().Single(
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(AiConversation));
        Assert.Equal(
            [nameof(AiActionDraft.ConversationId), nameof(AiActionDraft.UserId)],
            conversationForeignKey.Properties.Select(property => property.Name));
        Assert.Equal(
            [nameof(AiConversation.Id), nameof(AiConversation.UserId)],
            conversationForeignKey.PrincipalKey.Properties.Select(property => property.Name));
        Assert.Equal(DeleteBehavior.Restrict, conversationForeignKey.DeleteBehavior);

        Assert.Contains(
            entityType.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(User)
                          && foreignKey.Properties.Select(property => property.Name)
                              .SequenceEqual([nameof(AiActionDraft.UserId)]));
        Assert.Contains(
            entityType.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(User)
                          && foreignKey.Properties.Select(property => property.Name)
                              .SequenceEqual([nameof(AiActionDraft.ConfirmedByUserId)]));
    }

    [Fact]
    public void OrderDraft_ConfiguresThirtyMinuteExpiryIdempotencyAndConcurrency()
    {
        var entityType = GetEntityType<AiOrderDraft>();

        Assert.Equal(
            AiOrderDraftStatus.PendingConfirmation,
            entityType.FindProperty(nameof(AiOrderDraft.DraftStatus))!.GetDefaultValue());
        Assert.Equal(
            "CURRENT_TIMESTAMP + INTERVAL '30 minutes'",
            entityType.FindProperty(nameof(AiOrderDraft.ExpiresAt))!.GetDefaultValueSql());
        Assert.True(entityType.FindProperty(nameof(AiOrderDraft.ConcurrencyVersion))!.IsConcurrencyToken);

        var idempotencyIndex = entityType.GetIndexes().Single(
            index => index.GetDatabaseName() == "idx_ai_order_draft_user_idempotency");
        Assert.True(idempotencyIndex.IsUnique);
        Assert.Equal(
            [nameof(AiOrderDraft.UserId), nameof(AiOrderDraft.IdempotencyKey)],
            idempotencyIndex.Properties.Select(property => property.Name));
        Assert.True(entityType.GetIndexes().Single(
            index => index.GetDatabaseName() == "idx_ai_order_draft_sale_order_id").IsUnique);

        var conversationForeignKey = entityType.GetForeignKeys().Single(
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(AiConversation));
        Assert.Equal(
            [nameof(AiOrderDraft.ConversationId), nameof(AiOrderDraft.UserId)],
            conversationForeignKey.Properties.Select(property => property.Name));
        Assert.Equal(
            [nameof(AiConversation.Id), nameof(AiConversation.UserId)],
            conversationForeignKey.PrincipalKey.Properties.Select(property => property.Name));
        Assert.Equal(DeleteBehavior.Restrict, conversationForeignKey.DeleteBehavior);

        AssertForeignKey<AiOrderDraft, User>([nameof(AiOrderDraft.UserId)], DeleteBehavior.Restrict);
        AssertForeignKey<AiOrderDraft, Customer>([nameof(AiOrderDraft.CustomerId)], DeleteBehavior.Restrict);
        AssertForeignKey<AiOrderDraft, Quotation>([nameof(AiOrderDraft.QuotationId)], DeleteBehavior.SetNull);
        AssertForeignKey<AiOrderDraft, Ware>([nameof(AiOrderDraft.WareId)], DeleteBehavior.SetNull);
        AssertForeignKey<AiOrderDraft, SaleOrder>([nameof(AiOrderDraft.SaleOrderId)], DeleteBehavior.SetNull);
    }

    [Fact]
    public void OrderDraftDetail_UsesGlobalPrecisionAndConfirmationSnapshotConstraints()
    {
        var entityType = GetEntityType<AiOrderDraftDetail>();

        Assert.Equal(
            NumericPrecision.QuantityScale,
            entityType.FindProperty(nameof(AiOrderDraftDetail.Quantity))!.GetScale());
        Assert.Equal(
            NumericPrecision.QuantityScale,
            entityType.FindProperty(nameof(AiOrderDraftDetail.BaseQuantity))!.GetScale());
        Assert.Equal(
            NumericPrecision.QuantityScale,
            entityType.FindProperty(nameof(AiOrderDraftDetail.UnitConversion))!.GetScale());
        Assert.Equal(
            NumericPrecision.QuantityScale,
            entityType.FindProperty(nameof(AiOrderDraftDetail.MinimumOrderQuantitySnapshot))!.GetScale());
        Assert.Equal(
            NumericPrecision.MoneyScale,
            entityType.FindProperty(nameof(AiOrderDraftDetail.FixedPrice))!.GetScale());

        var sortIndex = entityType.GetIndexes().Single(
            index => index.GetDatabaseName() == "idx_ai_order_draft_detail_draft_sort");
        Assert.True(sortIndex.IsUnique);
        Assert.Contains(
            entityType.GetCheckConstraints(),
            constraint => constraint.Name == "ck_ai_order_draft_detail_source_record");

        AssertForeignKey<AiOrderDraftDetail, AiOrderDraft>(
            [nameof(AiOrderDraftDetail.AiOrderDraftId)],
            DeleteBehavior.Cascade);
        AssertForeignKey<AiOrderDraftDetail, GoodsEntity>(
            [nameof(AiOrderDraftDetail.GoodsId)],
            DeleteBehavior.Restrict);
        AssertForeignKey<AiOrderDraftDetail, GoodsUnit>(
            [nameof(AiOrderDraftDetail.GoodsUnitId)],
            DeleteBehavior.Restrict);
    }

    [Fact]
    public void McpAccessToken_ConfiguresHashOnlyStorageAndUserIndexes()
    {
        var entityType = GetEntityType<McpAccessToken>();
        var tokenHash = entityType.FindProperty(nameof(McpAccessToken.TokenHash))!;

        Assert.Equal(64, tokenHash.GetMaxLength());
        Assert.True(tokenHash.IsFixedLength());
        Assert.True(entityType.GetIndexes().Single(
            index => index.GetDatabaseName() == "idx_mcp_access_token_hash").IsUnique);
        Assert.True(entityType.GetIndexes().Single(
            index => index.GetDatabaseName() == "idx_mcp_access_token_prefix").IsUnique);
        Assert.Null(typeof(McpAccessToken).GetProperty("Token"));
        Assert.Null(typeof(McpAccessToken).GetProperty("Secret"));
        AssertForeignKey<McpAccessToken, User>([nameof(McpAccessToken.UserId)], DeleteBehavior.Restrict);
    }

    [Fact]
    public void AiPersistenceModel_HasChineseCommentsForEveryTableAndColumn()
    {
        var entityTypes = new[]
        {
            GetEntityType<AiConversation>(),
            GetEntityType<AiMessage>(),
            GetEntityType<AiActionDraft>(),
            GetEntityType<AiOrderDraft>(),
            GetEntityType<AiOrderDraftDetail>(),
            GetEntityType<McpAccessToken>()
        };

        foreach (var entityType in entityTypes)
        {
            Assert.False(string.IsNullOrWhiteSpace(entityType.GetComment()));
            Assert.All(
                entityType.GetProperties(),
                property => Assert.False(
                    string.IsNullOrWhiteSpace(property.GetComment()),
                    $"{entityType.ClrType.Name}.{property.Name} 缺少数据库注释。"));
        }
    }

    [Fact]
    public void PersistenceDefaults_UseDocumentedDurations()
    {
        var before = DateTime.UtcNow;
        var conversation = new AiConversation();
        var actionDraft = new AiActionDraft();
        var draft = new AiOrderDraft();
        var after = DateTime.UtcNow;

        Assert.InRange(
            conversation.RetainUntil,
            before.AddDays(AiPersistenceDefaults.ConversationRetentionDays),
            after.AddDays(AiPersistenceDefaults.ConversationRetentionDays));
        Assert.InRange(
            actionDraft.ExpiresAt,
            before.AddMinutes(AiPersistenceDefaults.ActionDraftLifetimeMinutes),
            after.AddMinutes(AiPersistenceDefaults.ActionDraftLifetimeMinutes));
        Assert.InRange(
            draft.ExpiresAt,
            before.AddMinutes(AiPersistenceDefaults.OrderDraftLifetimeMinutes),
            after.AddMinutes(AiPersistenceDefaults.OrderDraftLifetimeMinutes));
    }

    private IEntityType GetEntityType<TEntity>()
    {
        return model.FindEntityType(typeof(TEntity))
               ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not part of the EF model.");
    }

    private void AssertForeignKey<TDependent, TPrincipal>(
        IReadOnlyList<string> propertyNames,
        DeleteBehavior deleteBehavior)
    {
        var foreignKey = GetEntityType<TDependent>().GetForeignKeys().Single(
            candidate => candidate.PrincipalEntityType.ClrType == typeof(TPrincipal)
                         && candidate.Properties.Select(property => property.Name).SequenceEqual(propertyNames));

        Assert.Equal(deleteBehavior, foreignKey.DeleteBehavior);
    }
}
