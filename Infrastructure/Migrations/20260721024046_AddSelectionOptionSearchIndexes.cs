using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectionOptionSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_goods_name_trgm ON goods USING gin (lower(name) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_goods_code_trgm ON goods USING gin (lower(code) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_customer_name_trgm ON customer USING gin (lower(name) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_customer_code_trgm ON customer USING gin (lower(code) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_supplier_name_trgm ON supplier USING gin (lower(name) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_supplier_code_trgm ON supplier USING gin (lower(code) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_quotation_name_trgm ON quotation USING gin (lower(name) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_quotation_code_trgm ON quotation USING gin (lower(code) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_customer_protocol_name_trgm ON customer_protocol USING gin (lower(name) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_customer_protocol_code_trgm ON customer_protocol USING gin (lower(code) gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_sale_order_order_no_trgm ON sale_order USING gin (lower(order_no) gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_sale_order_order_no_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_customer_protocol_code_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_customer_protocol_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_quotation_code_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_quotation_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_supplier_code_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_supplier_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_customer_code_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_customer_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_goods_code_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_goods_name_trgm;");

            // pg_trgm 可能由同库其他对象共享，回滚仅移除本迁移创建的索引，不删除扩展。
        }
    }
}
