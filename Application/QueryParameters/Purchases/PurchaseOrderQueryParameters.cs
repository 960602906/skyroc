using System.Linq.Expressions;
using Domain.Entities.Purchases;
using Shared.Constants;

namespace Application.QueryParameters;

/// <summary>
/// 采购单分页查询参数。
/// </summary>
public class PurchaseOrderQueryParameters : PagedQueryParameters
{
    /// <summary>
    /// 采购单编号或商品名称、编码关键字，采用包含匹配。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 预计到货起始时间（含），UTC。
    /// </summary>
    public DateTime? ReceiveTimeStart { get; set; }

    /// <summary>
    /// 预计到货截止时间（含），UTC。
    /// </summary>
    public DateTime? ReceiveTimeEnd { get; set; }

    /// <summary>
    /// 采购模式筛选。
    /// </summary>
    public PurchasePattern? PurchasePattern { get; set; }

    /// <summary>
    /// 采购单执行状态筛选。
    /// </summary>
    public PurchaseOrderStatus? BusinessStatus { get; set; }

    /// <summary>
    /// 供应商主键筛选。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 采购员主键筛选。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 商品主键筛选，命中包含该商品的采购单。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    /// 通用启用禁用状态筛选。
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    /// 构建采购单查询表达式。
    /// </summary>
    /// <returns>可交给仓储执行的组合筛选表达式。</returns>
    public Expression<Func<PurchaseOrder, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        return x =>
            (string.IsNullOrWhiteSpace(keyword)
             || x.PurchaseNo.Contains(keyword)
             || x.Details.Any(detail => detail.GoodsNameSnapshot.Contains(keyword)
                                        || detail.GoodsCodeSnapshot.Contains(keyword)))
            && (!ReceiveTimeStart.HasValue || x.ReceiveTime >= ReceiveTimeStart.Value)
            && (!ReceiveTimeEnd.HasValue || x.ReceiveTime <= ReceiveTimeEnd.Value)
            && (!PurchasePattern.HasValue || x.PurchasePattern == PurchasePattern.Value)
            && (!BusinessStatus.HasValue || x.BusinessStatus == BusinessStatus.Value)
            && (!SupplierId.HasValue || x.SupplierId == SupplierId.Value)
            && (!PurchaserId.HasValue || x.PurchaserId == PurchaserId.Value)
            && (!GoodsId.HasValue || x.Details.Any(detail => detail.GoodsId == GoodsId.Value))
            && (!Status.HasValue || x.Status == Status.Value);
    }
}
