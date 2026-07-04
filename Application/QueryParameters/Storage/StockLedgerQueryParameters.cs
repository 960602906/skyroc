using Domain.Entities.Storage;
using Domain.Queries.Storage;

namespace Application.QueryParameters;

/// <summary>
/// 库存台账分页参数，支持按仓库、商品、批次、方向、来源和发生时间筛选。
/// </summary>
public class StockLedgerQueryParameters : PagedQueryParameters
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
    /// 库存增加或减少方向筛选。
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

    /// <summary>
    /// 转换为仓储层使用的规范化库存台账条件。
    /// </summary>
    /// <returns>去除关键字首尾空白后的查询条件。</returns>
    public StockLedgerCriteria ToCriteria()
    {
        return new StockLedgerCriteria
        {
            Keyword = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim(),
            WareId = WareId,
            GoodsId = GoodsId,
            StockBatchId = StockBatchId,
            Direction = Direction,
            SourceType = SourceType,
            OccurredTimeStart = OccurredTimeStart,
            OccurredTimeEnd = OccurredTimeEnd
        };
    }
}
