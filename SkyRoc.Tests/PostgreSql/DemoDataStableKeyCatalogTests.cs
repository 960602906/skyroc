using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
///     验证长期联调生成器使用稳定且可识别的业务键，避免按人工名称模糊匹配。
/// </summary>
public class DemoDataStableKeyCatalogTests
{
    /// <summary>
    ///     同一领域和序号必须始终生成相同的精确业务键，并保持全局管理前缀。
    /// </summary>
    [Fact]
    public void Create_ReturnsDeterministicManagedKey_ForBusinessAreaAndSequence()
    {
        var first = DemoDataStableKeyCatalog.Create("goods", 7);
        var second = DemoDataStableKeyCatalog.Create("GOODS", 7);

        Assert.Equal("SKYROC-DEMO-GOODS-007", first);
        Assert.Equal(first, second);
        Assert.True(DemoDataStableKeyCatalog.IsManaged(first));
        Assert.False(DemoDataStableKeyCatalog.IsManaged("SKYROC-DEMOGRAPHY-GOODS-007"));
    }

    /// <summary>
    ///     领域码和序号不合法时必须在写入数据库前拒绝，避免产生无法稳定定位的记录。
    /// </summary>
    [Theory]
    [InlineData("goods type", 1)]
    [InlineData("商品", 1)]
    [InlineData("goods", 0)]
    public void Create_ThrowsArgumentException_WhenBusinessAreaOrSequenceIsInvalid(string businessArea, int sequence)
    {
        Assert.Throws<ArgumentException>(() => DemoDataStableKeyCatalog.Create(businessArea, sequence));
    }
}
