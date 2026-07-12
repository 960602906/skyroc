using System.Data;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     对专用 PostgreSQL 测试库执行只读基础质量扫描，为 T1 统一验收器提供底座。
/// </summary>
public sealed class DatabaseQualityReportGenerator(PostgreSqlTestSettings settings)
{
    private readonly DatabaseMetadataInventory _metadataInventory = new(settings);

    /// <summary>
    ///     扫描逐表数量、字段填充、状态、外键、业务编码、临时残留和基础一致性。
    /// </summary>
    public async Task<DataQualityReport> GenerateAsync(
        ApplicationDbContext context,
        string runId,
        CancellationToken cancellationToken = default)
    {
        var databaseName = DatabaseSafetyGuard.Validate(settings);
        var metadataInventory = await _metadataInventory.GenerateAsync(context, cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var connection = (NpgsqlConnection)context.Database.GetDbConnection();
            var columns = await ReadColumnsAsync(connection, cancellationToken);
            var tables = columns.Select(column => column.TableName).Distinct(StringComparer.Ordinal).Order().ToArray();
            var tableCounts = new Dictionary<string, long>(StringComparer.Ordinal);
            var fieldFillRates = new Dictionary<string, decimal>(StringComparer.Ordinal);
            var statusDistributions = new Dictionary<string, IReadOnlyDictionary<string, long>>(StringComparer.Ordinal);
            var duplicateBusinessCodes = new List<string>();
            var temporaryResidues = new List<string>();

            foreach (var table in tables)
            {
                var tableColumns = columns.Where(column => column.TableName == table).ToArray();
                var tableMetrics = await ReadTableMetricsAsync(connection, table, tableColumns, cancellationToken);
                tableCounts[table] = tableMetrics.TotalRows;
                foreach (var metric in tableMetrics.FieldFillRates)
                    fieldFillRates[$"{table}.{metric.Key}"] = metric.Value;
                foreach (var residue in tableMetrics.TemporaryResidues)
                    temporaryResidues.Add($"{table}.{residue.Key}={residue.Value}");

                foreach (var statusColumn in tableColumns.Where(column => IsStatusColumn(column.ColumnName)))
                {
                    statusDistributions[$"{table}.{statusColumn.ColumnName}"] =
                        await ReadDistributionAsync(connection, table, statusColumn.ColumnName, cancellationToken);
                }

                foreach (var codeColumn in tableColumns.Where(column =>
                             IsBusinessCodeColumn(column.ColumnName)
                             && !DataQualityRuleCatalog.IsDuplicateBusinessCodeExempt(table, column.ColumnName)))
                {
                    var duplicateCount = await CountDuplicateValuesAsync(
                        connection,
                        table,
                        codeColumn.ColumnName,
                        cancellationToken);
                    if (duplicateCount > 0)
                        duplicateBusinessCodes.Add($"{table}.{codeColumn.ColumnName}={duplicateCount}");
                }
            }

            var orphanForeignKeys = await ReadUnvalidatedForeignKeysAsync(connection, cancellationToken);
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
            var stockQuantitiesNonNegative = await CheckStockQuantitiesAsync(connection, tables, cancellationToken);
            var consistencyChecks = new Dictionary<string, bool>(metadataInventory.Checks, StringComparer.Ordinal)
            {
                ["migrationHistoryMatchesModel"] = !pendingMigrations.Any(),
                ["temporaryBatchResidueIsZero"] = temporaryResidues.Count == 0,
                ["foreignKeysAreValidated"] = orphanForeignKeys.Count == 0,
                ["stockBatchQuantitiesAreNonNegative"] = stockQuantitiesNonNegative
            };

            return DataQualityReport.CreateInfrastructureReport(
                runId,
                databaseName,
                tableCounts,
                fieldFillRates,
                statusDistributions,
                orphanForeignKeys,
                duplicateBusinessCodes,
                temporaryResidues,
                consistencyChecks,
                metadataInventory);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static async Task<IReadOnlyList<ColumnMetadata>> ReadColumnsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT table_name, column_name, data_type
                           FROM information_schema.columns
                           WHERE table_schema = 'public'
                             AND table_name <> '__EFMigrationsHistory'
                             AND table_name IN (
                                 SELECT table_name
                                 FROM information_schema.tables
                                 WHERE table_schema = 'public' AND table_type = 'BASE TABLE')
                           ORDER BY table_name, ordinal_position
                           """;
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = new List<ColumnMetadata>();
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnMetadata(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2)));
        }

        return columns;
    }

    private static async Task<TableMetrics> ReadTableMetricsAsync(
        NpgsqlConnection connection,
        string tableName,
        IReadOnlyList<ColumnMetadata> columns,
        CancellationToken cancellationToken)
    {
        var selections = new List<string> { "COUNT(*)" };
        foreach (var column in columns)
        {
            var quotedColumn = Quote(column.ColumnName);
            var presentCondition = IsTextColumn(column.DataType)
                ? $"{quotedColumn} IS NOT NULL AND BTRIM({quotedColumn}) <> ''"
                : $"{quotedColumn} IS NOT NULL";
            selections.Add($"COUNT(*) FILTER (WHERE {presentCondition})");
            if (IsTextColumn(column.DataType))
                selections.Add($"COUNT(*) FILTER (WHERE {quotedColumn} LIKE '{TestBatchContext.Prefix}%')");
        }

        await using var command = new NpgsqlCommand(
            $"SELECT {string.Join(", ", selections)} FROM {Quote(tableName)}",
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        var totalRows = reader.GetInt64(0);
        var fieldFillRates = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var temporaryResidues = new Dictionary<string, long>(StringComparer.Ordinal);
        var ordinal = 1;
        foreach (var column in columns)
        {
            var presentRows = reader.GetInt64(ordinal++);
            fieldFillRates[column.ColumnName] = totalRows == 0
                ? 100m
                : decimal.Round(presentRows * 100m / totalRows, 4, MidpointRounding.AwayFromZero);
            if (!IsTextColumn(column.DataType))
                continue;

            var residueCount = reader.GetInt64(ordinal++);
            if (residueCount > 0)
                temporaryResidues[column.ColumnName] = residueCount;
        }

        return new TableMetrics(totalRows, fieldFillRates, temporaryResidues);
    }

    private static async Task<IReadOnlyDictionary<string, long>> ReadDistributionAsync(
        NpgsqlConnection connection,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var quotedColumn = Quote(columnName);
        await using var command = new NpgsqlCommand(
            $"SELECT COALESCE({quotedColumn}::text, '<null>'), COUNT(*) FROM {Quote(tableName)} GROUP BY {quotedColumn} ORDER BY 1",
            connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var distribution = new Dictionary<string, long>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
            distribution[reader.GetString(0)] = reader.GetInt64(1);
        return distribution;
    }

    private static async Task<long> CountDuplicateValuesAsync(
        NpgsqlConnection connection,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var quotedColumn = Quote(columnName);
        var sql = $"""
                   SELECT COUNT(*)
                   FROM (
                       SELECT {quotedColumn}
                       FROM {Quote(tableName)}
                       WHERE {quotedColumn} IS NOT NULL AND BTRIM({quotedColumn}::text) <> ''
                       GROUP BY {quotedColumn}
                       HAVING COUNT(*) > 1
                   ) duplicates
                   """;
        await using var command = new NpgsqlCommand(sql, connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<IReadOnlyList<string>> ReadUnvalidatedForeignKeysAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT child.relname, constraint_info.conname
                           FROM pg_constraint constraint_info
                           JOIN pg_class child ON child.oid = constraint_info.conrelid
                           JOIN pg_namespace child_schema ON child_schema.oid = child.relnamespace
                           WHERE constraint_info.contype = 'f'
                             AND child_schema.nspname = 'public'
                             AND NOT constraint_info.convalidated
                           ORDER BY child.relname, constraint_info.conname
                           """;
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var findings = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
            findings.Add($"{reader.GetString(0)}.{reader.GetString(1)} is not validated");
        return findings;
    }

    private static async Task<bool> CheckStockQuantitiesAsync(
        NpgsqlConnection connection,
        IReadOnlyCollection<string> tables,
        CancellationToken cancellationToken)
    {
        if (!tables.Contains("stock_batch", StringComparer.Ordinal))
            return false;

        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM stock_batch WHERE current_quantity < 0 OR available_quantity < 0 OR available_quantity > current_quantity",
            connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) == 0;
    }

    private static bool IsTextColumn(string dataType)
    {
        return dataType is "character varying" or "character" or "text";
    }

    private static bool IsStatusColumn(string columnName)
    {
        return columnName.Equals("status", StringComparison.Ordinal)
               || columnName.EndsWith("_status", StringComparison.Ordinal);
    }

    private static bool IsBusinessCodeColumn(string columnName)
    {
        return columnName.Equals("code", StringComparison.Ordinal)
               || columnName.Equals("username", StringComparison.Ordinal)
               || columnName.EndsWith("_code", StringComparison.Ordinal)
               || columnName.EndsWith("_no", StringComparison.Ordinal);
    }

    private static string Quote(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private sealed record ColumnMetadata(string TableName, string ColumnName, string DataType);

    private sealed record TableMetrics(
        long TotalRows,
        IReadOnlyDictionary<string, decimal> FieldFillRates,
        IReadOnlyDictionary<string, long> TemporaryResidues);
}
