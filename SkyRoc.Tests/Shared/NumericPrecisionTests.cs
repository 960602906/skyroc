using Shared.Constants;
using Xunit;

namespace SkyRoc.Tests.Shared;

public class NumericPrecisionTests
{
    [Fact]
    public void GlobalPrecision_RoundsQuantityAndMoneyWithSharedScalesAndMode()
    {
        Assert.Equal(6, NumericPrecision.QuantityScale);
        Assert.Equal(4, NumericPrecision.MoneyScale);
        Assert.Equal(MidpointRounding.AwayFromZero, NumericPrecision.RoundingMode);
        Assert.Equal(1.234568m, NumericPrecision.RoundQuantity(1.2345675m));
        Assert.Equal(-1.2346m, NumericPrecision.RoundMoney(-1.23455m));
    }
}
