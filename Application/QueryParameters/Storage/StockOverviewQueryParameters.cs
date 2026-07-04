using Domain.Queries.Storage;

namespace Application.QueryParameters;

/// <summary>
/// 库存总览分页参数，支持按仓库、分类、商品和关键字筛选聚合库存。
/// </summary>
public class StockOverviewQueryParameters : PagedQueryParameters
{
    /// <summary>
    /// 商品名称或编码关键字，采用包含匹配。
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
    /// 是否包含当前账面数量合计为零的仓库商品分组；默认不包含。
    /// </summary>
    public bool IncludeZeroStock { get; set; }

    /// <summary>
    /// 转换为仓储层使用的规范化库存总览条件。
    /// </summary>
    /// <returns>去除关键字首尾空白后的查询条件。</returns>
    public StockOverviewCriteria ToCriteria()
    {
        return new StockOverviewCriteria
        {
            Keyword = Normalize(Keyword),
            WareId = WareId,
            GoodsTypeId = GoodsTypeId,
            GoodsId = GoodsId,
            IncludeZeroStock = IncludeZeroStock
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
