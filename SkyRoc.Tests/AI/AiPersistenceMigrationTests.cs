using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace SkyRoc.Tests.AI;

/// <summary>
/// 校验 AI 持久化迁移只创建和回滚本阶段的六张表及其完整注释。
/// </summary>
public class AiPersistenceMigrationTests
{
    private static readonly string[] ExpectedTables =
    [
        "ai_conversation",
        "ai_message",
        "ai_action_draft",
        "ai_order_draft",
        "ai_order_draft_detail",
        "mcp_access_token"
    ];

    [Fact]
    public void MigrationUp_CreatesSixCommentedTablesAndRequiredUniqueIndexes()
    {
        var migration = new TestableMigration();
        var operations = migration.BuildUp();
        var tables = operations.OfType<CreateTableOperation>().ToDictionary(operation => operation.Name);

        Assert.Equal(ExpectedTables.Order(), tables.Keys.Order());
        foreach (var table in tables.Values)
        {
            Assert.False(string.IsNullOrWhiteSpace(table.Comment));
            Assert.All(
                table.Columns,
                column => Assert.False(
                    string.IsNullOrWhiteSpace(column.Comment),
                    $"{table.Name}.{column.Name} 缺少迁移列注释。"));
        }

        var indexes = operations.OfType<CreateIndexOperation>().ToDictionary(operation => operation.Name);
        Assert.True(indexes["idx_ai_message_conversation_sequence"].IsUnique);
        Assert.True(indexes["idx_ai_action_draft_user_idempotency"].IsUnique);
        Assert.True(indexes["idx_ai_order_draft_user_idempotency"].IsUnique);
        Assert.True(indexes["idx_ai_order_draft_sale_order_id"].IsUnique);
        Assert.True(indexes["idx_ai_order_draft_detail_draft_sort"].IsUnique);
        Assert.True(indexes["idx_mcp_access_token_hash"].IsUnique);
        Assert.True(indexes["idx_mcp_access_token_prefix"].IsUnique);

        Assert.Equal(
            "CURRENT_TIMESTAMP + INTERVAL '30 days'",
            tables["ai_conversation"].Columns.Single(column => column.Name == "retain_until").DefaultValueSql);
        Assert.Equal(
            "CURRENT_TIMESTAMP + INTERVAL '30 minutes'",
            tables["ai_action_draft"].Columns.Single(column => column.Name == "expires_at").DefaultValueSql);
        Assert.Equal(
            "CURRENT_TIMESTAMP + INTERVAL '30 minutes'",
            tables["ai_order_draft"].Columns.Single(column => column.Name == "expires_at").DefaultValueSql);
    }

    [Fact]
    public void MigrationDown_DropsOnlyThisStageTables()
    {
        var migration = new TestableMigration();
        var operations = migration.BuildDown();
        var droppedTables = operations.OfType<DropTableOperation>().Select(operation => operation.Name);

        Assert.Equal(ExpectedTables.Order(), droppedTables.Order());
        Assert.All(operations, operation => Assert.IsType<DropTableOperation>(operation));
    }

    private sealed class TestableMigration : AddAiPersistenceTables
    {
        public IReadOnlyList<MigrationOperation> BuildUp()
        {
            var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            Up(builder);
            return builder.Operations;
        }

        public IReadOnlyList<MigrationOperation> BuildDown()
        {
            var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            Down(builder);
            return builder.Operations;
        }
    }
}
