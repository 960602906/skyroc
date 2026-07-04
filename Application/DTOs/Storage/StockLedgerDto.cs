using Domain.Entities.Storage;

namespace Application.DTOs.Storage;

/// <summary>
/// 库存台账流水，记录一次已生效库存增减及其来源、成本和批次余额。
/// </summary>
public class StockLedgerDto
{
    /// <summary>
    /// 库存流水主键。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 被调整的库存批次主键。
    /// </summary>
    public Guid StockBatchId { get; set; }

    /// <summary>
    /// 流水发生仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 流水发生时的仓库名称快照。
    /// </summary>
    public string WareName { get; set; } = string.Empty;

    /// <summary>
    /// 流水对应商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 流水发生时的商品名称快照。
    /// </summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>
    /// 流水发生时的商品编码快照。
    /// </summary>
    public string GoodsCode { get; set; } = string.Empty;

    /// <summary>
    /// 流水发生时的批次号快照。
    /// </summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>
    /// 流水发生时的基础单位名称快照。
    /// </summary>
    public string BaseUnitName { get; set; } = string.Empty;

    /// <summary>
    /// 库存增加或减少方向。
    /// </summary>
    public StockLedgerDirection Direction { get; set; }

    /// <summary>
    /// 触发库存变化的业务来源类型。
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
    /// 本次库存变化的绝对数量，始终为正数并按基础单位计量。
    /// </summary>
    public decimal ChangeQuantity { get; set; }

    /// <summary>
    /// 带方向的变化数量；增加为正数、减少为负数，按基础单位计量。
    /// </summary>
    public decimal SignedChangeQuantity => Direction == StockLedgerDirection.Increase
        ? ChangeQuantity
        : -ChangeQuantity;

    /// <summary>
    /// 本流水生效后的批次账面数量，按基础单位计量。
    /// </summary>
    public decimal BalanceQuantity { get; set; }

    /// <summary>
    /// 本次变化采用的单位成本，按系统业务币种和基础单位计量。
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// 本次变化数量对应的成本绝对值，按系统业务币种计量。
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// 库存变化实际生效时间（UTC）。
    /// </summary>
    public DateTime OccurredTime { get; set; }

    /// <summary>
    /// 被本流水反向回滚的原流水主键；普通流水为空。
    /// </summary>
    public Guid? ReversedFromLedgerId { get; set; }

    /// <summary>
    /// 流水业务说明或反审核原因。
    /// </summary>
    public string? Remark { get; set; }
}
