namespace Domain.Queries.Storage;

/// <summary>
/// 库存批次查询条件，用于定位仓库商品批次及其效期和余额。
/// </summary>
public class StockBatchCriteria
{
    /// <summary>
    /// 商品名称、商品编码或批次号关键字，采用包含匹配。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 仓库主键筛选。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    /// 商品分类主键筛选。
    /// </summary>
    public Guid? GoodsTypeId { get; set; }

    /// <summary>
    /// 商品主键筛选。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    /// 精确批次号筛选；为空时不限制批次号。
    /// </summary>
    public string? BatchNo { get; set; }

    /// <summary>
    /// 到期日期起始值（含）；仅匹配已设置到期日期的批次。
    /// </summary>
    public DateOnly? ExpireDateStart { get; set; }

    /// <summary>
    /// 到期日期截止值（含）；仅匹配已设置到期日期的批次。
    /// </summary>
    public DateOnly? ExpireDateEnd { get; set; }

    /// <summary>
    /// 是否包含当前账面数量为零的批次。
    /// </summary>
    public bool IncludeZeroStock { get; set; }
}
