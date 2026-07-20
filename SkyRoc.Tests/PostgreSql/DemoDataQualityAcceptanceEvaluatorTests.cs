using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
///     验证长期联调数据验收器会阻止数量不足、必填字段缺失和基础一致性异常的数据集。
/// </summary>
public class DemoDataQualityAcceptanceEvaluatorTests
{
    /// <summary>
    ///     数量、字段和一致性任一不达标时，验收器必须给出可定位的失败原因。
    /// </summary>
    [Fact]
    public void Evaluate_ReturnsDetailedFindings_WhenReportDoesNotMeetDemoDataBaseline()
    {
        var report = DataQualityReport.CreateInfrastructureReport(
            "SKYROC-DEMO-QUALITY-UNIT",
            "skyroc",
            new Dictionary<string, long> { ["goods"] = 19 },
            new Dictionary<string, decimal> { ["goods.name"] = 95m },
            new Dictionary<string, IReadOnlyDictionary<string, long>>(),
            ["goods.fk_goods_type"],
            [],
            [],
            new Dictionary<string, bool> { ["stockBatchQuantitiesAreNonNegative"] = false },
            CreateMetadataInventory());

        var result = DemoDataQualityAcceptanceEvaluator.Evaluate(report);

        Assert.False(result.IsReady);
        Assert.Contains("goods：记录数 19，小于验收下限 30", result.Findings);
        Assert.Contains("goods.name：适用字段填充率 95%，应为 100%", result.Findings);
        Assert.Contains("存在孤儿外键：goods.fk_goods_type", result.Findings);
        Assert.Contains("业务一致性检查失败：stockBatchQuantitiesAreNonNegative", result.Findings);
    }

    private static MetadataInventoryResult CreateMetadataInventory()
    {
        return new MetadataInventoryResult(
            [
                new MetadataTableInventory(
                    "goods",
                    "商品档案",
                    [
                        new MetadataColumnInventory(
                            "name",
                            false,
                            null,
                            null,
                            null,
                            "商品名称",
                            DataQualityFieldApplicability.AlwaysRequired)
                    ],
                    [],
                    [],
                    new DataQualityTableRule(DataQualityTableCategory.MasterData, 30, 80, "商品主数据"))
            ],
            [],
            new Dictionary<string, bool>());
    }
}
