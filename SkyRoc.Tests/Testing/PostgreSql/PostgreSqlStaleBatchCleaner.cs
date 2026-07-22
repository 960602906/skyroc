using System.Data;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     清理所有历史轮次遗留的 <c>SKYROC-AUTOTEST-</c> 临时业务数据。
///     先按文本列前缀定位种子行，再沿外键向下扩展无前缀的阻断子行，最后按叶子优先删除。
///     本入口仅用于 T14 质量门禁收口，严禁触及非临时前缀或其它业务内容。
/// </summary>
public sealed class PostgreSqlStaleBatchCleaner(PostgreSqlTestSettings settings)
{
    /// <summary>
    ///     在事务中删除历史临时残留及其外键阻断子行。
    /// </summary>
    public async Task CleanAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        DatabaseSafetyGuard.Validate(settings);

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var connection = (NpgsqlConnection)context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync(cancellationToken);

                var dbTransaction = (NpgsqlTransaction)transaction.GetDbTransaction();
                var textColumnsByTable = await ReadTextColumnsByTableAsync(
                    connection, dbTransaction, cancellationToken);
                var primaryKeys = await ReadPrimaryKeyColumnsAsync(
                    connection, dbTransaction, cancellationToken);
                var foreignKeys = await ReadForeignKeysAsync(
                    connection, dbTransaction, cancellationToken);

                var doomed = await CollectSeedRowsAsync(
                    connection, dbTransaction, textColumnsByTable, primaryKeys, cancellationToken);
                await ExpandDependentRowsAsync(
                    connection, dbTransaction, doomed, foreignKeys, primaryKeys, cancellationToken);

                var orderedTables = ResolveLeafFirstOrder(doomed.Keys, foreignKeys);
                foreach (var tableName in orderedTables)
                {
                    if (!doomed.TryGetValue(tableName, out var ids) || ids.Count == 0)
                        continue;
                    if (!primaryKeys.TryGetValue(tableName, out var pkColumns) || pkColumns.Count != 1)
                    {
                        throw new InvalidOperationException(
                            $"表 {tableName} 需要单一主键才能按 ID 精确清理历史临时残留。");
                    }

                    await DeleteByIdsAsync(
                        connection, dbTransaction, tableName, pkColumns[0], ids, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private static async Task<Dictionary<string, HashSet<Guid>>> CollectSeedRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyDictionary<string, IReadOnlyList<string>> textColumnsByTable,
        IReadOnlyDictionary<string, IReadOnlyList<string>> primaryKeys,
        CancellationToken cancellationToken)
    {
        var doomed = new Dictionary<string, HashSet<Guid>>(StringComparer.Ordinal);
        foreach (var (tableName, textColumns) in textColumnsByTable)
        {
            if (textColumns.Count == 0)
                continue;
            if (!primaryKeys.TryGetValue(tableName, out var pkColumns) || pkColumns.Count != 1)
                continue;

            var predicates = textColumns
                .Select(column => $"{Quote(column)} LIKE '{TestBatchContext.Prefix}%'")
                .ToArray();
            var sql =
                $"SELECT {Quote(pkColumns[0])} FROM {Quote(tableName)} WHERE {string.Join(" OR ", predicates)}";
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.IsDBNull(0))
                    continue;
                var id = reader.GetGuid(0);
                if (!doomed.TryGetValue(tableName, out var ids))
                {
                    ids = [];
                    doomed[tableName] = ids;
                }

                ids.Add(id);
            }
        }

        return doomed;
    }

    /// <summary>
    ///     将引用已判定删除行的子表行一并纳入删除集，避免 RESTRICT 外键阻断。
    /// </summary>
    private static async Task ExpandDependentRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Dictionary<string, HashSet<Guid>> doomed,
        IReadOnlyList<ForeignKeyEdge> foreignKeys,
        IReadOnlyDictionary<string, IReadOnlyList<string>> primaryKeys,
        CancellationToken cancellationToken)
    {
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var edge in foreignKeys)
            {
                if (edge.ChildColumns.Count != 1 || edge.ParentColumns.Count != 1)
                    continue;
                if (!primaryKeys.TryGetValue(edge.ChildTable, out var childPk) || childPk.Count != 1)
                    continue;
                if (!doomed.TryGetValue(edge.ParentTable, out var parentIds) || parentIds.Count == 0)
                    continue;

                var childIds = await ReadChildIdsReferencingParentsAsync(
                    connection,
                    transaction,
                    edge.ChildTable,
                    childPk[0],
                    edge.ChildColumns[0],
                    parentIds,
                    cancellationToken);
                if (childIds.Count == 0)
                    continue;

                if (!doomed.TryGetValue(edge.ChildTable, out var existing))
                {
                    existing = [];
                    doomed[edge.ChildTable] = existing;
                }

                foreach (var childId in childIds)
                {
                    if (existing.Add(childId))
                        changed = true;
                }
            }
        }
    }

    private static async Task<IReadOnlyList<Guid>> ReadChildIdsReferencingParentsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string childTable,
        string childPrimaryKeyColumn,
        string childFkColumn,
        IReadOnlyCollection<Guid> parentIds,
        CancellationToken cancellationToken)
    {
        var sql = $"""
                   SELECT {Quote(childPrimaryKeyColumn)}
                   FROM {Quote(childTable)}
                   WHERE {Quote(childFkColumn)} = ANY(@parentIds)
                   """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("parentIds", parentIds.ToArray());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ids = new List<Guid>();
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
                ids.Add(reader.GetGuid(0));
        }

        return ids;
    }

    private static IReadOnlyList<string> ResolveLeafFirstOrder(
        IEnumerable<string> tableNames,
        IReadOnlyList<ForeignKeyEdge> foreignKeys)
    {
        var tables = new HashSet<string>(tableNames, StringComparer.Ordinal);
        var dependents = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var remainingParents = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var table in tables)
        {
            dependents[table] = new HashSet<string>(StringComparer.Ordinal);
            remainingParents[table] = 0;
        }

        foreach (var edge in foreignKeys)
        {
            if (!tables.Contains(edge.ChildTable) || !tables.Contains(edge.ParentTable))
                continue;
            if (edge.ChildTable == edge.ParentTable)
                continue;
            if (dependents[edge.ParentTable].Add(edge.ChildTable))
                remainingParents[edge.ChildTable]++;
        }

        var queue = new PriorityQueue<string, string>();
        foreach (var pair in remainingParents.Where(pair => pair.Value == 0))
            queue.Enqueue(pair.Key, pair.Key);

        var parentFirst = new List<string>(tables.Count);
        while (queue.Count > 0)
        {
            var parent = queue.Dequeue();
            parentFirst.Add(parent);
            foreach (var child in dependents[parent].Order(StringComparer.Ordinal))
            {
                remainingParents[child]--;
                if (remainingParents[child] == 0)
                    queue.Enqueue(child, child);
            }
        }

        var ordered = new List<string>(tables.Count);
        foreach (var table in tables.Order(StringComparer.Ordinal))
        {
            if (!parentFirst.Contains(table, StringComparer.Ordinal))
                ordered.Add(table);
        }

        parentFirst.Reverse();
        ordered.AddRange(parentFirst);
        return ordered;
    }

    private static async Task DeleteByIdsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string tableName,
        string primaryKeyColumn,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return;

        await using var command = new NpgsqlCommand(
            $"DELETE FROM {Quote(tableName)} WHERE {Quote(primaryKeyColumn)} = ANY(@ids)",
            connection,
            transaction);
        command.Parameters.AddWithValue("ids", ids.ToArray());
        _ = await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ReadTextColumnsByTableAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT c.relname AS table_name, a.attname AS column_name
                           FROM pg_attribute a
                           JOIN pg_class c ON c.oid = a.attrelid
                           JOIN pg_namespace n ON n.oid = c.relnamespace
                           JOIN pg_type t ON t.oid = a.atttypid
                           WHERE n.nspname = 'public'
                             AND c.relkind = 'r'
                             AND a.attnum > 0
                             AND NOT a.attisdropped
                             AND t.typname IN ('varchar', 'text', 'bpchar', 'citext')
                           ORDER BY c.relname, a.attname
                           """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
        {
            var tableName = reader.GetString(0);
            var columnName = reader.GetString(1);
            if (!map.TryGetValue(tableName, out var columns))
            {
                columns = [];
                map[tableName] = columns;
            }

            columns.Add(columnName);
        }

        return map.ToDictionary(
            pair => pair.Key,
            IReadOnlyList<string> (pair) => pair.Value,
            StringComparer.Ordinal);
    }

    private static async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ReadPrimaryKeyColumnsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT c.relname AS table_name, a.attname AS column_name, a.attnum
                           FROM pg_index i
                           JOIN pg_class c ON c.oid = i.indrelid
                           JOIN pg_namespace n ON n.oid = c.relnamespace
                           JOIN LATERAL unnest(i.indkey) WITH ORDINALITY AS key_col(attnum, ordinality)
                               ON TRUE
                           JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = key_col.attnum
                           WHERE n.nspname = 'public'
                             AND c.relkind = 'r'
                             AND i.indisprimary
                           ORDER BY c.relname, key_col.ordinality
                           """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
        {
            var tableName = reader.GetString(0);
            var columnName = reader.GetString(1);
            if (!map.TryGetValue(tableName, out var columns))
            {
                columns = [];
                map[tableName] = columns;
            }

            columns.Add(columnName);
        }

        return map.ToDictionary(
            pair => pair.Key,
            IReadOnlyList<string> (pair) => pair.Value,
            StringComparer.Ordinal);
    }

    private static async Task<IReadOnlyList<ForeignKeyEdge>> ReadForeignKeysAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT
                               child.relname AS child_table,
                               parent.relname AS parent_table,
                               child_cols.attname AS child_column,
                               parent_cols.attname AS parent_column,
                               cols.ordinality
                           FROM pg_constraint constraint_info
                           JOIN pg_class child ON child.oid = constraint_info.conrelid
                           JOIN pg_namespace child_schema ON child_schema.oid = child.relnamespace
                           JOIN pg_class parent ON parent.oid = constraint_info.confrelid
                           JOIN pg_namespace parent_schema ON parent_schema.oid = parent.relnamespace
                           JOIN LATERAL unnest(constraint_info.conkey, constraint_info.confkey)
                               WITH ORDINALITY AS cols(child_attnum, parent_attnum, ordinality) ON TRUE
                           JOIN pg_attribute child_cols
                               ON child_cols.attrelid = child.oid AND child_cols.attnum = cols.child_attnum
                           JOIN pg_attribute parent_cols
                               ON parent_cols.attrelid = parent.oid AND parent_cols.attnum = cols.parent_attnum
                           WHERE constraint_info.contype = 'f'
                             AND child_schema.nspname = 'public'
                             AND parent_schema.nspname = 'public'
                           ORDER BY child.relname, parent.relname, constraint_info.oid, cols.ordinality
                           """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var edges = new Dictionary<(string Child, string Parent), (List<string> ChildCols, List<string> ParentCols)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var childTable = reader.GetString(0);
            var parentTable = reader.GetString(1);
            var childColumn = reader.GetString(2);
            var parentColumn = reader.GetString(3);
            var key = (childTable, parentTable);
            if (!edges.TryGetValue(key, out var columns))
            {
                columns = ([], []);
                edges[key] = columns;
            }

            columns.ChildCols.Add(childColumn);
            columns.ParentCols.Add(parentColumn);
        }

        return edges
            .Select(pair => new ForeignKeyEdge(
                pair.Key.Child,
                pair.Key.Parent,
                pair.Value.ChildCols,
                pair.Value.ParentCols))
            .ToArray();
    }

    private static string Quote(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private sealed record ForeignKeyEdge(
        string ChildTable,
        string ParentTable,
        IReadOnlyList<string> ChildColumns,
        IReadOnlyList<string> ParentColumns);
}
