namespace Application.DTOs.Reports;

/// <summary>
/// 采购出入库供应商汇总响应，统计供应商维度的采购入库与采购退货出库。
/// </summary>
public class PurchaseInOutSupplierSummaryDto
{
    /// <summary>供应商主键；采购业务未指定供应商时为空。</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>供应商名称快照；未指定供应商时为“未指定供应商”。</summary>
    public string SupplierName { get; set; } = string.Empty;

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
