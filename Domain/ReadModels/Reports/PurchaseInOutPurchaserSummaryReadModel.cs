namespace Domain.ReadModels.Reports;

/// <summary>
/// 采购出入库采购员汇总读模型。
/// </summary>
public sealed class PurchaseInOutPurchaserSummaryReadModel
{
    /// <summary>采购员主键；采购业务未指定采购员时为空。</summary>
    public Guid? PurchaserId { get; init; }

    /// <summary>采购员名称快照。</summary>
    public string PurchaserName { get; init; } = string.Empty;

    /// <summary>采购入库基础数量合计。</summary>
    public decimal InBaseQuantity { get; init; }

    /// <summary>采购入库金额合计。</summary>
    public decimal InAmount { get; init; }

    /// <summary>采购退货出库基础数量合计。</summary>
    public decimal OutBaseQuantity { get; init; }

    /// <summary>采购退货出库金额合计。</summary>
    public decimal OutAmount { get; init; }
}
