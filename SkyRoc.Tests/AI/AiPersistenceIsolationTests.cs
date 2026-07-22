using System.Text.Json;
using Domain.Entities.AI;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SkyRoc.Tests.AI;

/// <summary>
/// 校验 AI 会话按当前用户隔离，并通过消息序号执行无 offset 的游标读取。
/// </summary>
public class AiPersistenceIsolationTests
{
    [Fact]
    public async Task ConversationMessages_FilterByOwnerAndUseSequenceCursor()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ai-isolation-{Guid.NewGuid():N}")
            .Options;
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        await using (var arrangeContext = new ApplicationDbContext(options))
        {
            arrangeContext.AiConversations.AddRange(
                new AiConversation
                {
                    Id = conversationId,
                    UserId = ownerId,
                    Title = "当前用户会话"
                },
                new AiConversation
                {
                    Id = Guid.NewGuid(),
                    UserId = otherUserId,
                    Title = "其他用户会话"
                });
            arrangeContext.AiMessages.AddRange(
                BuildMessage(conversationId, 3, "第三条"),
                BuildMessage(conversationId, 1, "第一条"),
                BuildMessage(conversationId, 2, "第二条"));
            await arrangeContext.SaveChangesAsync();
        }

        await using var assertContext = new ApplicationDbContext(options);
        var ownerMessages = await LoadMessagePageAsync(assertContext, ownerId, conversationId, 1, 20);
        var otherUserMessages = await LoadMessagePageAsync(assertContext, otherUserId, conversationId, 0, 20);

        Assert.Equal([2L, 3L], ownerMessages.Select(message => message.Sequence));
        Assert.Empty(otherUserMessages);
    }

    [Fact]
    public void ActionDraftHash_IsCanonicalAndRejectsChangedOperationArgumentsOrUser()
    {
        var ownerId = Guid.NewGuid();
        const string operationId = "CustomersController.Update";
        const string originalArguments = """{"name":"食堂","id":"42","amount":1.00}""";
        var canonicalArguments = AiActionDraftIntegrity.CanonicalizeArguments(originalArguments);
        var draft = new AiActionDraft
        {
            UserId = ownerId,
            OperationId = operationId,
            CanonicalArgumentsJson = canonicalArguments,
            ArgumentsHash = AiActionDraftIntegrity.ComputeHash(ownerId, operationId, canonicalArguments)
        };

        Assert.Equal(
            """{"amount":1,"id":"42","name":"食堂"}""",
            canonicalArguments);
        Assert.True(draft.MatchesConfirmation(
            ownerId,
            operationId,
            """{"amount":1,"id":"42","name":"食堂"}"""));
        Assert.False(draft.MatchesConfirmation(ownerId, "CustomersController.Delete", originalArguments));
        Assert.False(draft.MatchesConfirmation(
            ownerId,
            operationId,
            """{"name":"食堂","id":"43","amount":1}"""));
        Assert.False(draft.MatchesConfirmation(Guid.NewGuid(), operationId, originalArguments));
        Assert.Throws<JsonException>(() =>
            AiActionDraftIntegrity.CanonicalizeArguments("""{"id":"42","id":"43"}"""));
    }

    private static AiMessage BuildMessage(Guid conversationId, long sequence, string content)
    {
        return new AiMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = AiMessageRole.Assistant,
            Content = content,
            Sequence = sequence,
            MessageStatus = AiMessageStatus.Completed
        };
    }

    private static Task<List<AiMessage>> LoadMessagePageAsync(
        ApplicationDbContext context,
        Guid currentUserId,
        Guid conversationId,
        long afterSequence,
        int limit)
    {
        return context.AiMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId
                              && message.Conversation.UserId == currentUserId
                              && message.Sequence > afterSequence)
            .OrderBy(message => message.Sequence)
            .Take(limit)
            .ToListAsync();
    }
}
