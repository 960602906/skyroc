namespace Application.DTOs.Storage;

/// <summary>
/// 出库商品明细 DTO，返回来源订单行、商品、单位、扣减批次、数量和价格快照。
/// </summary>
public class StockOutDetailDto : BaseDto
{
    /// <summary>
    /// 所属出库主单主键。
    /// </summary>
    public Guid StockOutOrderId { get; set; }

    /// <summary>
    /// 来源销售订单商品明细主键；手工销售出库及非销售出库为空。
    /// </summary>
    public Guid? SaleOrderDetailId { get; set; }

    /// <summary>
    /// 被扣减的库存批次主键。
    /// </summary>
    public Guid? StockBatchId { get; set; }

    /// <summary>
    /// 出库商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 出库发生时的商品名称快照。
    /// </summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>
    /// 出库发生时的商品编码快照。
    /// </summary>
    public string GoodsCode { get; set; } = string.Empty;

    /// <summary>
    /// 出库计量单位主键。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 出库发生时的计量单位名称快照。
    /// </summary>
    public string GoodsUnitName { get; set; } = string.Empty;

    /// <summary>
    /// 出库单位换算为商品基础单位的比例。
    /// </summary>
    public decimal ConversionRate { get; set; }

    /// <summary>
    /// 按出库单位计量的商品数量。
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 按商品基础单位换算后的出库数量。
    /// </summary>
    public decimal BaseQuantity { get; set; }

    /// <summary>
    /// 出库业务单价，按系统业务币种和出库单位计量。
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 出库业务金额，为出库数量乘以单价后的金额快照。
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// 选定库存批次的批次号快照。
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 当前出库商品行的业务备注。
    /// </summary>
    public string? Remark { get; set; }
}
