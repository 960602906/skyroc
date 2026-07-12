using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     将 EF Core 运行时模型与 PostgreSQL 系统目录逐项核对，并为每张业务表建立数据质量规则。
/// </summary>
public sealed class DatabaseMetadataInventory(PostgreSqlTestSettings settings)
{
    /// <summary>
    ///     只读生成业务表、列、约束、注释和字段适用性盘点结果。
    /// </summary>
    public async Task<MetadataInventoryResult> GenerateAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        DatabaseSafetyGuard.Validate(settings);
        var modelTables = ReadModelTables(context.GetService<IDesignTimeModel>().Model);
        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var connection = (NpgsqlConnection)context.Database.GetDbConnection();
            var catalog = await PostgreSqlCatalog.ReadAsync(connection, cancellationToken);
            var result = Compare(modelTables, catalog);
            return result with
            {
                Tables = modelTables
                    .OrderBy(table => table.TableName, StringComparer.Ordinal)
                    .Select(table => table with
                    {
                        Columns = table.Columns
                            .Select(column => column with
                            {
                                Applicability = DataQualityRuleCatalog.GetApplicability(table.TableName, column)
                            })
                            .ToArray(),
                        Rule = DataQualityRuleCatalog.CreateRule(table)
                    })
                    .ToArray()
            };
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private static IReadOnlyList<MetadataTableInventory> ReadModelTables(IModel model)
    {
        var tables = new Dictionary<string, MutableTable>(StringComparer.Ordinal);
        foreach (var entityType in model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(tableName))
                continue;

            var schema = entityType.GetSchema() ?? "public";
            if (!string.Equals(schema, "public", StringComparison.Ordinal))
                continue;

            var storeObject = StoreObjectIdentifier.Table(tableName, schema);
            if (!tables.TryGetValue(tableName, out var table))
            {
                table = new MutableTable(tableName, entityType.GetComment());
                tables.Add(tableName, table);
            }

            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (string.IsNullOrWhiteSpace(columnName))
                    continue;

                table.Columns[columnName] = new MetadataColumnInventory(
                    columnName,
                    property.IsNullable,
                    property.GetPrecision(),
                    property.GetScale(),
                    property.GetDefaultValueSql(),
                    property.GetComment(),
                    DataQualityFieldApplicability.NotConfigured);
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var principalTableName = foreignKey.PrincipalEntityType.GetTableName();
                if (string.IsNullOrWhiteSpace(principalTableName))
                    continue;

                var principalStoreObject = StoreObjectIdentifier.Table(
                    principalTableName,
                    foreignKey.PrincipalEntityType.GetSchema() ?? "public");
                var constraintName = foreignKey.GetConstraintName(storeObject, principalStoreObject);
                if (!string.IsNullOrWhiteSpace(constraintName))
                    table.ForeignKeyNames.Add(constraintName);
            }

            foreach (var index in entityType.GetIndexes().Where(index => index.IsUnique))
            {
                var databaseName = index.GetDatabaseName(storeObject);
                if (!string.IsNullOrWhiteSpace(databaseName))
                    table.UniqueConstraintNames.Add(databaseName);
            }
        }

        return tables.Values
            .Select(table => new MetadataTableInventory(
                table.TableName,
                table.Comment,
                table.Columns.Values.OrderBy(column => column.ColumnName, StringComparer.Ordinal).ToArray(),
                table.ForeignKeyNames.Order(StringComparer.Ordinal).ToArray(),
                table.UniqueConstraintNames.Order(StringComparer.Ordinal).ToArray(),
                null))
            .ToArray();
    }

    private static MetadataInventoryResult Compare(
        IReadOnlyList<MetadataTableInventory> modelTables,
        PostgreSqlCatalog catalog)
    {
        var findings = new List<string>();
        var modelByName = modelTables.ToDictionary(table => table.TableName, StringComparer.Ordinal);
        var catalogByName = catalog.Tables.ToDictionary(table => table.TableName, StringComparer.Ordinal);

        AddSetDifferences(findings, "EF 缺少 PostgreSQL 业务表", catalogByName.Keys, modelByName.Keys);
        AddSetDifferences(findings, "PostgreSQL 缺少 EF 业务表", modelByName.Keys, catalogByName.Keys);

        var commentsMatch = true;
        var columnsMatch = true;
        var foreignKeysMatch = true;
        var uniqueConstraintsMatch = true;
        foreach (var (tableName, modelTable) in modelByName)
        {
            if (!catalogByName.TryGetValue(tableName, out var catalogTable))
                continue;

            if (string.IsNullOrWhiteSpace(catalogTable.Comment))
            {
                commentsMatch = false;
                findings.Add($"{tableName} 缺少表注释");
            }
            else if (!string.IsNullOrWhiteSpace(modelTable.Comment)
                     && !string.Equals(modelTable.Comment, catalogTable.Comment, StringComparison.Ordinal))
            {
                commentsMatch = false;
                findings.Add($"{tableName} 表注释与 EF 模型不一致");
            }

            var modelColumns = modelTable.Columns.ToDictionary(column => column.ColumnName, StringComparer.Ordinal);
            var catalogColumns = catalogTable.Columns.ToDictionary(column => column.ColumnName, StringComparer.Ordinal);
            var beforeColumnFindings = findings.Count;
            AddSetDifferences(findings, $"{tableName} EF 缺少 PostgreSQL 列", catalogColumns.Keys, modelColumns.Keys);
            AddSetDifferences(findings, $"{tableName} PostgreSQL 缺少 EF 列", modelColumns.Keys, catalogColumns.Keys);
            foreach (var (columnName, modelColumn) in modelColumns)
            {
                if (!catalogColumns.TryGetValue(columnName, out var catalogColumn))
                    continue;

                if (modelColumn.IsNullable != catalogColumn.IsNullable)
                    findings.Add($"{tableName}.{columnName} 可空性与 EF 模型不一致");
                if (modelColumn.Precision is not null
                    && (modelColumn.Precision != catalogColumn.NumericPrecision
                        || modelColumn.Scale != catalogColumn.NumericScale))
                {
                    findings.Add($"{tableName}.{columnName} 数值精度与 EF 模型不一致");
                }

                if (string.IsNullOrWhiteSpace(catalogColumn.Comment))
                {
                    commentsMatch = false;
                    findings.Add($"{tableName}.{columnName} 缺少列注释");
                }
                else if (!string.IsNullOrWhiteSpace(modelColumn.Comment)
                         && !string.Equals(modelColumn.Comment, catalogColumn.Comment, StringComparison.Ordinal))
                {
                    commentsMatch = false;
                    findings.Add($"{tableName}.{columnName} 列注释与 EF 模型不一致");
                }
            }

            columnsMatch &= findings.Count == beforeColumnFindings;
            if (!modelTable.ForeignKeyNames.All(catalogTable.ForeignKeyNames.Contains))
            {
                foreignKeysMatch = false;
                findings.Add($"{tableName} 外键约束与 EF 模型不一致");
            }

            if (!modelTable.UniqueConstraintNames.All(catalogTable.UniqueConstraintNames.Contains))
            {
                uniqueConstraintsMatch = false;
                findings.Add($"{tableName} 唯一约束与 EF 模型不一致");
            }
        }

        var rules = modelTables.Select(DataQualityRuleCatalog.CreateRule).ToArray();
        var allTablesHaveRules = rules.All(rule => rule is not null);
        var allColumnsHaveRules = modelTables.All(table =>
            table.Columns.All(column => DataQualityRuleCatalog.GetApplicability(table.TableName, column) != DataQualityFieldApplicability.NotConfigured));
        if (!allTablesHaveRules)
            findings.Add("存在未分类业务表");
        if (!allColumnsHaveRules)
            findings.Add("存在未定义适用性的持久化字段");

        var checks = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["efModelMatchesPostgreSqlCatalog"] = columnsMatch && modelByName.Keys.ToHashSet().SetEquals(catalogByName.Keys),
            ["allBusinessTablesHaveQualityRules"] = allTablesHaveRules,
            ["allPersistedColumnsHaveApplicabilityRules"] = allColumnsHaveRules,
            ["databaseCommentsMatchModel"] = commentsMatch,
            ["foreignKeysMatchModel"] = foreignKeysMatch,
            ["uniqueConstraintsMatchModel"] = uniqueConstraintsMatch
        };

        return new MetadataInventoryResult([], findings, checks);
    }

    private static void AddSetDifferences(
        ICollection<string> findings,
        string heading,
        IEnumerable<string> expected,
        IEnumerable<string> actual)
    {
        var missing = expected.Except(actual, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (missing.Length > 0)
            findings.Add($"{heading}：{string.Join("、", missing)}");
    }

    private sealed class MutableTable(string tableName, string? comment)
    {
        public string TableName { get; } = tableName;
        public string? Comment { get; } = comment;
        public Dictionary<string, MetadataColumnInventory> Columns { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ForeignKeyNames { get; } = new(StringComparer.Ordinal);
        public HashSet<string> UniqueConstraintNames { get; } = new(StringComparer.Ordinal);
    }

    private sealed record PostgreSqlCatalog(IReadOnlyList<PostgreSqlTable> Tables)
    {
        public static async Task<PostgreSqlCatalog> ReadAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            const string tableSql = """
                                    SELECT table_info.relname, obj_description(table_info.oid, 'pg_class')
                                    FROM pg_class table_info
                                    JOIN pg_namespace schema_info ON schema_info.oid = table_info.relnamespace
                                    WHERE schema_info.nspname = 'public'
                                      AND table_info.relkind = 'r'
                                      AND table_info.relname <> '__EFMigrationsHistory'
                                    ORDER BY table_info.relname
                                    """;
            const string columnSql = """
                                     SELECT table_info.relname, column_info.attname, NOT column_info.attnotnull,
                                            information.data_type, information.numeric_precision, information.numeric_scale,
                                            col_description(table_info.oid, column_info.attnum)
                                     FROM pg_attribute column_info
                                     JOIN pg_class table_info ON table_info.oid = column_info.attrelid
                                     JOIN pg_namespace schema_info ON schema_info.oid = table_info.relnamespace
                                     JOIN information_schema.columns information
                                       ON information.table_schema = schema_info.nspname
                                      AND information.table_name = table_info.relname
                                      AND information.column_name = column_info.attname
                                     WHERE schema_info.nspname = 'public'
                                       AND table_info.relkind = 'r'
                                       AND table_info.relname <> '__EFMigrationsHistory'
                                       AND column_info.attnum > 0
                                       AND NOT column_info.attisdropped
                                     ORDER BY table_info.relname, column_info.attnum
                                     """;
            const string foreignKeySql = """
                                         SELECT table_info.relname, constraint_info.conname
                                         FROM pg_constraint constraint_info
                                         JOIN pg_class table_info ON table_info.oid = constraint_info.conrelid
                                         JOIN pg_namespace schema_info ON schema_info.oid = table_info.relnamespace
                                         WHERE schema_info.nspname = 'public' AND constraint_info.contype = 'f'
                                         """;
            const string uniqueSql = """
                                     SELECT table_info.relname, index_info.relname
                                     FROM pg_index relation_index
                                     JOIN pg_class table_info ON table_info.oid = relation_index.indrelid
                                     JOIN pg_class index_info ON index_info.oid = relation_index.indexrelid
                                     JOIN pg_namespace schema_info ON schema_info.oid = table_info.relnamespace
                                     WHERE schema_info.nspname = 'public'
                                       AND relation_index.indisunique
                                       AND NOT relation_index.indisprimary
                                     """;
            var tables = new Dictionary<string, MutablePostgreSqlTable>(StringComparer.Ordinal);
            await using (var command = new NpgsqlCommand(tableSql, connection))
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                    tables[reader.GetString(0)] = new MutablePostgreSqlTable(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1));
            }

            await using (var command = new NpgsqlCommand(columnSql, connection))
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var table = tables[reader.GetString(0)];
                    table.Columns.Add(new PostgreSqlColumn(
                        reader.GetString(1),
                        reader.GetBoolean(2),
                        reader.GetString(3),
                        reader.IsDBNull(4) ? null : reader.GetInt32(4),
                        reader.IsDBNull(5) ? null : reader.GetInt32(5),
                        reader.IsDBNull(6) ? null : reader.GetString(6)));
                }
            }

            await ReadConstraintNamesAsync(connection, foreignKeySql, tables, true, cancellationToken);
            await ReadConstraintNamesAsync(connection, uniqueSql, tables, false, cancellationToken);
            return new PostgreSqlCatalog(tables.Values.Select(table => table.ToImmutable()).ToArray());
        }

        private static async Task ReadConstraintNamesAsync(
            NpgsqlConnection connection,
            string sql,
            IReadOnlyDictionary<string, MutablePostgreSqlTable> tables,
            bool foreignKey,
            CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var table = tables[reader.GetString(0)];
                if (foreignKey)
                    table.ForeignKeyNames.Add(reader.GetString(1));
                else
                    table.UniqueConstraintNames.Add(reader.GetString(1));
            }
        }
    }

    private sealed class MutablePostgreSqlTable(string tableName, string? comment)
    {
        public string TableName { get; } = tableName;
        public string? Comment { get; } = comment;
        public List<PostgreSqlColumn> Columns { get; } = [];
        public HashSet<string> ForeignKeyNames { get; } = new(StringComparer.Ordinal);
        public HashSet<string> UniqueConstraintNames { get; } = new(StringComparer.Ordinal);

        public PostgreSqlTable ToImmutable() => new(
            TableName,
            Comment,
            Columns,
            ForeignKeyNames,
            UniqueConstraintNames);
    }

    private sealed record PostgreSqlTable(
        string TableName,
        string? Comment,
        IReadOnlyList<PostgreSqlColumn> Columns,
        IReadOnlySet<string> ForeignKeyNames,
        IReadOnlySet<string> UniqueConstraintNames);

    private sealed record PostgreSqlColumn(
        string ColumnName,
        bool IsNullable,
        string DataType,
        int? NumericPrecision,
        int? NumericScale,
        string? Comment);
}
