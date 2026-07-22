using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
/// 校验数据质量规则对默认关闭且尚未造数的功能表采用显式分类。
/// </summary>
public class DataQualityRuleCatalogTests
{
    /// <summary>
    /// P6-02 只建立持久化结构，六张 AI/MCP 表在业务服务交付前允许为空。
    /// </summary>
    /// <param name="tableName">待分类的 AI/MCP 表名。</param>
    [Theory]
    [InlineData("ai_conversation")]
    [InlineData("ai_message")]
    [InlineData("ai_action_draft")]
    [InlineData("ai_order_draft")]
    [InlineData("ai_order_draft_detail")]
    [InlineData("mcp_access_token")]
    public void CreateRule_FeatureGatedAiTablesAllowEmptyUntilServicesExist(string tableName)
    {
        var table = new MetadataTableInventory(tableName, "测试表", [], [], [], null);

        var rule = DataQualityRuleCatalog.CreateRule(table);

        Assert.Equal(DataQualityTableCategory.FeatureGated, rule.Category);
        Assert.Equal(0, rule.MinimumRows);
        Assert.Contains("允许业务表为空", rule.Rationale, StringComparison.Ordinal);
    }
}
