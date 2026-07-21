using System.Data.Common;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
///     在专用 PostgreSQL 回滚事务中验证万级选项搜索的返回上限与 trigram 执行计划。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class SelectionOptionSearchPerformancePostgreSqlTests(PostgreSqlTestFixture fixture)
{
    /// <summary>
    ///     临时插入一万条商品后只读取 limit+1，并验证名称包含搜索可使用本次 GIN 索引；事务结束后自动回滚。
    /// </summary>
    [Fact]
    public async Task Goods_selection_search_limits_response_and_uses_trigram_index_for_ten_thousand_rows()
    {
        await fixture.ExecuteInRollbackTransactionAsync(async context =>
        {
            var goodsTypeId = await context.GoodsTypes.AsNoTracking().Select(x => x.Id).FirstAsync();
            var prefix = $"OPT{Guid.NewGuid():N}"[..20];
            await context.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO goods (id, name, code, goods_type_id, is_on_sale, create_time, status)
                SELECT gen_random_uuid(),
                       {{prefix}} || '-商品-' || lpad(value::text, 5, '0'),
                       {{prefix}} || lpad(value::text, 5, '0'),
                       {{goodsTypeId}},
                       TRUE,
                       CURRENT_TIMESTAMP,
                       1
                FROM generate_series(1, 10000) AS value;
                """);

            var options = await new GoodsRepository(context).SearchSelectionOptionsAsync($"{prefix}-商品-099", 21);

            Assert.InRange(options.Count, 1, 21);
            Assert.All(options, option => Assert.StartsWith(prefix, option.Label));

            await context.Database.ExecuteSqlRawAsync("SET LOCAL enable_seqscan = off;");
            var plan = await ReadExplainPlanAsync(context, $"%{prefix.ToLowerInvariant()}-商品-099%");
            Assert.Contains("idx_goods_name_trgm", plan, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static async Task<string> ReadExplainPlanAsync(Infrastructure.Data.ApplicationDbContext context, string keyword)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "EXPLAIN SELECT id FROM goods WHERE lower(name) LIKE @keyword LIMIT 21;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "keyword";
        parameter.Value = keyword;
        command.Parameters.Add(parameter);

        await using DbDataReader reader = await command.ExecuteReaderAsync();
        var lines = new List<string>();
        while (await reader.ReadAsync())
        {
            lines.Add(reader.GetString(0));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
