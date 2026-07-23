namespace Application.DTOs.Storage;

/// <summary>
/// 入库商品行创建请求，描述入库单位、批次、数量和价格。
/// </summary>
public class CreateStockInDetailDto
{
    /// <summary>
    /// 来源采购单商品明细主键；仅采购入库回填，用于追溯到货来源。
    /// </summary>
    public Guid? PurchaseOrderDetailId { get; set; }

    /// <summary>
    /// 来源售后取货任务主键；仅销售退货入库使用，任务必须已完成且最多入库一次。
    /// </summary>
    public Guid? PickupTaskId { get; set; }

    /// <summary>
    /// 入库商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 入库计量单位主键，必须属于入库商品。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 按入库单位计量的入库数量，必须大于零。
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 入库单价，按入库单位计量，不得为负。
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 商品生产日期，仅记录自然日；未知时可为空。
    /// </summary>
    public DateOnly? ProductDate { get; set; }

    /// <summary>
    /// 商品到期日期，仅记录自然日；无保质期或未知时可为空。
    /// </summary>
    public DateOnly? ExpireDate { get; set; }

    /// <summary>
    /// 当前入库商品行的业务备注。
    /// </summary>
    public string? Remark { get; set; }
}
