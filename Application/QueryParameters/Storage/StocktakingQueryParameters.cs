using System.Linq.Expressions;
using Domain.Entities.Storage;
using Shared.Constants;

namespace Application.QueryParameters;

/// <summary>
/// 库存盘点分页查询参数，支持按编号、仓库、状态、商品和盘点时间筛选。
/// </summary>
public class StocktakingQueryParameters : PagedQueryParameters
{
    /// <summary>
    /// 盘点单编号或商品名称、编码关键字，采用包含匹配。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 盘点业务状态筛选。
    /// </summary>
    public StockDocumentStatus? BusinessStatus { get; set; }

    /// <summary>
    /// 被盘点仓库主键筛选。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    /// 被盘点商品主键筛选，命中包含该商品的盘点单。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    /// 盘点快照时间起始（含），UTC。
    /// </summary>
    public DateTime? StocktakingTimeStart { get; set; }

    /// <summary>
    /// 盘点快照时间截止（含），UTC。
    /// </summary>
    public DateTime? StocktakingTimeEnd { get; set; }

    /// <summary>
    /// 构建盘点单组合筛选表达式。
    /// </summary>
    /// <returns>可交给盘点仓储执行的查询条件。</returns>
    public Expression<Func<StocktakingOrder, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        return x =>
            (string.IsNullOrWhiteSpace(keyword)
             || x.StocktakingNo.Contains(keyword)
             || x.Details.Any(detail => detail.GoodsNameSnapshot.Contains(keyword)
                                        || detail.GoodsCodeSnapshot.Contains(keyword)))
            && (!BusinessStatus.HasValue || x.BusinessStatus == BusinessStatus.Value)
            && (!WareId.HasValue || x.WareId == WareId.Value)
            && (!GoodsId.HasValue || x.Details.Any(detail => detail.GoodsId == GoodsId.Value))
            && (!StocktakingTimeStart.HasValue || x.StocktakingTime >= StocktakingTimeStart.Value)
            && (!StocktakingTimeEnd.HasValue || x.StocktakingTime <= StocktakingTimeEnd.Value);
    }
}
