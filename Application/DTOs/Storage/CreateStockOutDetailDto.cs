namespace Application.DTOs.Storage;

/// <summary>
/// 出库商品行创建请求，指定扣减批次、出库单位、数量和业务价格。
/// </summary>
public class CreateStockOutDetailDto
{
    /// <summary>
    /// 来源销售订单商品明细主键；关联销售订单的销售出库必须填写。
    /// </summary>
    public Guid? SaleOrderDetailId { get; set; }

    /// <summary>
    /// 待扣减库存批次主键；批次必须属于出库仓库且商品与来源订单行一致。
    /// </summary>
    public Guid StockBatchId { get; set; }

    /// <summary>
    /// 出库计量单位主键，必须属于批次对应商品。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 按出库单位计量的商品数量，必须大于零。
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 出库业务单价，按出库单位和系统业务币种计量，不得为负。
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 当前出库商品行的业务备注。
    /// </summary>
    public string? Remark { get; set; }
}
