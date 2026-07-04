using System.Linq.Expressions;
using Domain.Entities.Storage;
using Shared.Constants;

namespace Application.QueryParameters;

/// <summary>
/// 入库单分页查询参数，按入库类型、仓库、业务方、时间和审核状态筛选。
/// </summary>
public class StockInOrderQueryParameters : PagedQueryParameters
{
    /// <summary>
    /// 入库单编号或商品名称、编码关键字，采用包含匹配。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 入库业务类型筛选：采购、其他或销售退货。
    /// </summary>
    public StockInOrderType? OrderType { get; set; }

    /// <summary>
    /// 单据业务状态筛选。
    /// </summary>
    public StockDocumentStatus? BusinessStatus { get; set; }

    /// <summary>
    /// 仓库主键筛选。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    /// 供应商主键筛选。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 客户主键筛选。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 商品主键筛选，命中包含该商品的入库单。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    /// 入库时间起始（含），UTC。
    /// </summary>
    public DateTime? InTimeStart { get; set; }

    /// <summary>
    /// 入库时间截止（含），UTC。
    /// </summary>
    public DateTime? InTimeEnd { get; set; }

    /// <summary>
    /// 构建入库单查询表达式；固定按入库类型约束筛选范围。
    /// </summary>
    /// <param name="orderType">当前接口对应的入库业务类型。</param>
    /// <returns>可交给仓储执行的组合筛选表达式。</returns>
    public Expression<Func<StockInOrder, bool>> QueryBuild(StockInOrderType orderType)
    {
        var keyword = Keyword?.Trim();
        return x =>
            x.OrderType == orderType
            && (string.IsNullOrWhiteSpace(keyword)
                || x.InNo.Contains(keyword)
                || x.Details.Any(detail => detail.GoodsNameSnapshot.Contains(keyword)
                                           || detail.GoodsCodeSnapshot.Contains(keyword)))
            && (!BusinessStatus.HasValue || x.BusinessStatus == BusinessStatus.Value)
            && (!WareId.HasValue || x.WareId == WareId.Value)
            && (!SupplierId.HasValue || x.SupplierId == SupplierId.Value)
            && (!CustomerId.HasValue || x.CustomerId == CustomerId.Value)
            && (!GoodsId.HasValue || x.Details.Any(detail => detail.GoodsId == GoodsId.Value))
            && (!InTimeStart.HasValue || x.InTime >= InTimeStart.Value)
            && (!InTimeEnd.HasValue || x.InTime <= InTimeEnd.Value);
    }
}
