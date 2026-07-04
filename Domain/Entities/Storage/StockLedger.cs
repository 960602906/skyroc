namespace Domain.Entities.Storage;

/// <summary>
/// 库存流水，以只追加方式记录每次审核或反审核对批次余额的影响。
/// </summary>
public class StockLedger : BaseEntity
{
    /// <summary>
    /// 被调整的库存批次主键。
    /// </summary>
    public Guid StockBatchId { get; set; }

    /// <summary>
    /// 流水发生仓库主键，冗余保存以支持台账筛选。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 流水发生时的仓库名称快照。
    /// </summary>
    public string WareNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 流水对应商品主键，冗余保存以支持商品台账查询。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 流水发生时的商品名称快照。
    /// </summary>
    public string GoodsNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 流水发生时的商品编码快照。
    /// </summary>
    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 流水发生时的批次号快照。
    /// </summary>
    public string BatchNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 流水发生时的基础单位名称快照。
    /// </summary>
    public string BaseUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 库存增减方向；数量本身始终为正数。
    /// </summary>
    public StockLedgerDirection Direction { get; set; }

    /// <summary>
    /// 触发库存变更的业务来源类型。
    /// </summary>
    public StockLedgerSourceType SourceType { get; set; }

    /// <summary>
    /// 来源入库、出库或盘点主单主键。
    /// </summary>
    public Guid SourceOrderId { get; set; }

    /// <summary>
    /// 来源入库、出库或盘点明细主键。
    /// </summary>
    public Guid SourceDetailId { get; set; }

    /// <summary>
    /// 本次变更数量，必须为正数并按基础单位计量。
    /// </summary>
    public decimal ChangeQuantity { get; set; }

    /// <summary>
    /// 本流水生效后的批次账面数量，按基础单位计量。
    /// </summary>
    public decimal BalanceQuantity { get; set; }

    /// <summary>
    /// 本次变更采用的单位成本，按系统业务币种和基础单位计量。
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// 流水对应库存变更总成本，为数量与单位成本的金额快照。
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// 库存变更实际生效时间（UTC）。
    /// </summary>
    public DateTime OccurredTime { get; set; }

    /// <summary>
    /// 被本流水回滚的原流水主键；普通流水为空，反向流水必须填写。
    /// </summary>
    public Guid? ReversedFromLedgerId { get; set; }

    /// <summary>
    /// 流水业务说明或反审核原因。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 被调整的库存批次。
    /// </summary>
    public virtual StockBatch StockBatch { get; set; } = null!;

    /// <summary>
    /// 被本流水回滚的原流水。
    /// </summary>
    public virtual StockLedger? ReversedFromLedger { get; set; }
}
