using Domain.Entities.Storage;

namespace Domain.Queries.Storage;

/// <summary>
/// 库存台账查询条件，用于追溯指定仓库、商品、批次和业务来源的增减流水。
/// </summary>
public class StockLedgerCriteria
{
    /// <summary>
    /// 仓库、商品、商品编码或批次号关键字，采用包含匹配。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 仓库主键筛选。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    /// 商品主键筛选。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    /// 库存批次主键筛选。
    /// </summary>
    public Guid? StockBatchId { get; set; }

    /// <summary>
    /// 库存增减方向筛选。
    /// </summary>
    public StockLedgerDirection? Direction { get; set; }

    /// <summary>
    /// 触发库存变化的业务来源类型筛选。
    /// </summary>
    public StockLedgerSourceType? SourceType { get; set; }

    /// <summary>
    /// 库存变化生效时间起始（含），UTC。
    /// </summary>
    public DateTime? OccurredTimeStart { get; set; }

    /// <summary>
    /// 库存变化生效时间截止（含），UTC。
    /// </summary>
    public DateTime? OccurredTimeEnd { get; set; }
}
