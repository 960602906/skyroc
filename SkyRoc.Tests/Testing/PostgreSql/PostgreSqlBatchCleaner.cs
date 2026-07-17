using System.Data;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     使用实体主键和本轮归属值双重条件，按外键逆序精确清理临时数据。
/// </summary>
public sealed class PostgreSqlBatchCleaner(PostgreSqlTestSettings settings)
{
    /// <summary>
    ///     清理当前批次已登记实体；不存在的记录视为幂等成功，归属不匹配则拒绝。
    /// </summary>
    public async Task CleanupAsync(
        ApplicationDbContext context,
        BatchCleanupRegistry registry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(registry);
        DatabaseSafetyGuard.Validate(settings);

        // 与生产 EnableRetryOnFailure 对齐：手动事务必须包在执行策略内
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var entry in registry.GetCleanupOrder())
                    await DeleteEntryAsync(context, entry, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private static async Task DeleteEntryAsync(
        ApplicationDbContext context,
        BatchCleanupEntry entry,
        CancellationToken cancellationToken)
    {
        var entityType = context.Model.FindEntityType(entry.EntityType)
                         ?? throw new InvalidOperationException($"Entity type '{entry.EntityType.Name}' is not mapped.");
        var tableName = entityType.GetTableName()
                        ?? throw new InvalidOperationException($"Entity type '{entry.EntityType.Name}' has no table.");
        var schema = entityType.GetSchema() ?? "public";
        var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
        var primaryKey = entityType.FindPrimaryKey()?.Properties.SingleOrDefault()
                         ?? throw new InvalidOperationException(
                             $"Entity type '{entry.EntityType.Name}' must have a single primary key.");
        var ownershipProperty = entityType.FindProperty(entry.OwnershipPropertyName)
                                ?? throw new InvalidOperationException(
                                    $"Ownership property '{entry.OwnershipPropertyName}' is not mapped on '{entry.EntityType.Name}'.");
        if (ownershipProperty.ClrType != typeof(string))
            throw new InvalidOperationException("Cleanup ownership properties must be strings.");

        var primaryKeyColumn = primaryKey.GetColumnName(storeObject)
                               ?? throw new InvalidOperationException("The primary key column is not mapped.");
        var ownershipColumn = ownershipProperty.GetColumnName(storeObject)
                              ?? throw new InvalidOperationException("The ownership column is not mapped.");
        var quotedTable = $"{Quote(schema)}.{Quote(tableName)}";

        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var dbTransaction = (NpgsqlTransaction)context.Database.CurrentTransaction!.GetDbTransaction();
        await using var selectCommand = new NpgsqlCommand(
            $"SELECT {Quote(ownershipColumn)} FROM {quotedTable} WHERE {Quote(primaryKeyColumn)} = @id",
            connection,
            dbTransaction);
        selectCommand.Parameters.AddWithValue("id", entry.EntityId);
        var actualOwnership = await selectCommand.ExecuteScalarAsync(cancellationToken);
        if (actualOwnership is null || actualOwnership is DBNull)
            return;
        if (!string.Equals(Convert.ToString(actualOwnership), entry.OwnershipValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cleanup ownership mismatch for '{entry.EntityType.Name}/{entry.EntityId}'.");
        }

        await using var deleteCommand = new NpgsqlCommand(
            $"DELETE FROM {quotedTable} WHERE {Quote(primaryKeyColumn)} = @id AND {Quote(ownershipColumn)} = @ownership",
            connection,
            dbTransaction);
        deleteCommand.Parameters.AddWithValue("id", entry.EntityId);
        deleteCommand.Parameters.AddWithValue("ownership", entry.OwnershipValue);
        var affectedRows = await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        if (affectedRows != 1)
            throw new DBConcurrencyException($"Expected one cleanup row but deleted {affectedRows}.");
    }

    private static string Quote(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
