namespace Domain.ReadModels.Reports;

/// <summary>
/// 采购出入库供应商汇总读模型。
/// </summary>
public sealed class PurchaseInOutSupplierSummaryReadModel
{
    /// <summary>供应商主键；采购业务未指定供应商时为空。</summary>
    public Guid? SupplierId { get; init; }

    /// <summary>供应商名称快照。</summary>
    public string SupplierName { get; init; } = string.Empty;

    /// <summary>采购入库基础数量合计。</summary>
    public decimal InBaseQuantity { get; init; }

    /// <summary>采购入库金额合计。</summary>
    public decimal InAmount { get; init; }

    /// <summary>采购退货出库基础数量合计。</summary>
    public decimal OutBaseQuantity { get; init; }

    /// <summary>采购退货出库金额合计。</summary>
    public decimal OutAmount { get; init; }
}
