namespace Shared.Constants;

/// <summary>
/// 业务数值的全局精度与舍入规则，确保订单、采购和库存计算采用一致口径。
/// </summary>
public static class NumericPrecision
{
    /// <summary>
    /// 数量、单位换算率及库存余额保留的小数位数。
    /// </summary>
    public const int QuantityScale = 6;

    /// <summary>
    /// 单价、单位成本及金额保留的小数位数。
    /// </summary>
    public const int MoneyScale = 4;

    /// <summary>
    /// 业务计算统一采用的中点舍入模式。
    /// </summary>
    public const MidpointRounding RoundingMode = MidpointRounding.AwayFromZero;

    /// <summary>
    /// 按全局数量精度舍入计算结果。
    /// </summary>
    /// <param name="value">待舍入的数量、换算率或库存余额。</param>
    /// <returns>保留全局数量小数位数的结果。</returns>
    public static decimal RoundQuantity(decimal value)
    {
        return decimal.Round(value, QuantityScale, RoundingMode);
    }

    /// <summary>
    /// 按全局金额精度舍入计算结果。
    /// </summary>
    /// <param name="value">待舍入的单价、成本或金额。</param>
    /// <returns>保留全局金额小数位数的结果。</returns>
    public static decimal RoundMoney(decimal value)
    {
        return decimal.Round(value, MoneyScale, RoundingMode);
    }
}
