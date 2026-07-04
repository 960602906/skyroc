namespace Domain.Queries.Storage;

/// <summary>
/// 库存总览查询条件，用于按仓库、分类和商品筛选批次聚合结果。
/// </summary>
public class StockOverviewCriteria
{
    /// <summary>
    /// 商品名称或编码关键字，采用包含匹配。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 仓库主键筛选；为空时查询全部仓库。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    /// 商品分类主键筛选；为空时查询全部分类。
    /// </summary>
    public Guid? GoodsTypeId { get; set; }

    /// <summary>
    /// 商品主键筛选；为空时查询全部商品。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    /// 是否包含当前账面数量为零的库存分组。
    /// </summary>
    public bool IncludeZeroStock { get; set; }
}
