namespace Domain.Entities.Traceability;

/// <summary>
/// 检测报告结论，标识入库商品质量检测的最终判定结果。
/// </summary>
public enum InspectionConclusion
{
    /// <summary>
    /// 待定：检测尚未出具结论或结果待复核。
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 合格：商品通过质量检测，可正常销售和溯源展示。
    /// </summary>
    Qualified = 2,

    /// <summary>
    /// 不合格：商品未通过质量检测，需按业务规则处理。
    /// </summary>
    Unqualified = 3
}
