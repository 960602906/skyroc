namespace Application.DTOs.Reports;

/// <summary>
/// 采购出入库采购员汇总响应，统计采购员维度的采购入库与采购退货出库。
/// </summary>
public class PurchaseInOutPurchaserSummaryDto
{
    /// <summary>采购员主键；采购业务未指定采购员时为空。</summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>采购员名称快照；未指定采购员时为“未指定采购员”。</summary>
    public string PurchaserName { get; set; } = string.Empty;

    /// <summary>采购入库基础数量合计。</summary>
    public decimal InBaseQuantity { get; set; }

    /// <summary>采购入库金额合计，按系统业务币种计量。</summary>
    public decimal InAmount { get; set; }

    /// <summary>采购退货出库基础数量合计。</summary>
    public decimal OutBaseQuantity { get; set; }

    /// <summary>采购退货出库金额合计，按系统业务币种计量。</summary>
    public decimal OutAmount { get; set; }

    /// <summary>采购入库金额减采购退货金额后的净采购金额。</summary>
    public decimal NetAmount { get; set; }
}
