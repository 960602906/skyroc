namespace Application.DTOs.Reports;

/// <summary>
/// 采购出入库商品汇总响应，统计采购入库与采购退货出库在商品维度的数量和金额。
/// </summary>
public class PurchaseInOutGoodsSummaryDto
{
    /// <summary>商品主键。</summary>
    public Guid GoodsId { get; set; }

    /// <summary>商品名称快照。</summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>商品编码快照。</summary>
    public string GoodsCode { get; set; } = string.Empty;

    /// <summary>计量单位名称快照；数量字段已按基础数量口径汇总。</summary>
    public string? BaseUnitName { get; set; }

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
