using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

public class SelectionOptionSearchMigrationTests
{
    [Fact]
    public void Migration_creates_pg_trgm_indexes_and_down_keeps_shared_extension()
    {
        var migration = new TestableMigration();
        var up = migration.BuildUp();
        var down = migration.BuildDown();
        var upSql = string.Join('\n', up.OfType<SqlOperation>().Select(x => x.Sql));
        var downSql = string.Join('\n', down.OfType<SqlOperation>().Select(x => x.Sql));
        var databaseOperation = Assert.Single(up.OfType<AlterDatabaseOperation>());

        Assert.Contains(databaseOperation.GetAnnotations(), annotation =>
            annotation.Name == "Npgsql:PostgresExtension:pg_trgm");
        Assert.Contains("idx_goods_name_trgm", upSql);
        Assert.Contains("idx_customer_code_trgm", upSql);
        Assert.Contains("idx_sale_order_order_no_trgm", upSql);
        Assert.Contains("gin_trgm_ops", upSql);
        Assert.Contains("DROP INDEX IF EXISTS idx_sale_order_order_no_trgm", downSql);
        Assert.DoesNotContain("DROP EXTENSION", downSql, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestableMigration : AddSelectionOptionSearchIndexes
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
